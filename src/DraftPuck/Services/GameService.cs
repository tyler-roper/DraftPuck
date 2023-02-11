using DraftPuck.Models.NhlApi;
using DraftPuck.Models.NhlApi.Helpers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace DraftPuck.Services
{
    public class GameService : IGameService
    {
        private readonly INhlApiService _nhlApi;
        private readonly ILogger<GameService> _logger;
        private readonly IGameCache _gameCache;
        private readonly DraftPuckContext _dbContext;
        private readonly ILobbyService _lobbyService;
        private readonly ILobbyEventService _lobbyEventService;
        static readonly Random _random = new Random();

        public GameService(INhlApiService nhlApi, ILogger<GameService> logger, IGameCache gameCache, DraftPuckContext dbContext, ILobbyService lobbyService, ILobbyEventService lobbyEventService)
        {
            _nhlApi = nhlApi;
            _logger = logger;
            _gameCache = gameCache;
            _dbContext = dbContext;
            _lobbyService = lobbyService;
            _lobbyEventService = lobbyEventService;
        }

        public async Task CheckGames()
        {
            RemoveOldGamesFromCache();

            var cachedGames = _gameCache.GetAllGames();
            if (cachedGames.Count == 0 || ((DateTime.UtcNow.Minute == 0 || DateTime.UtcNow.Minute == 30) && DateTime.UtcNow.Second > 0 && DateTime.UtcNow.Second <= 10))
            {
                await AddAllScheduledGamesToCache();
                return;
            }

            foreach (var game in cachedGames)
                await UpdateGame(game);
        }

        private async Task UpdateGame(LiveGame game)
        {
            if (!ShouldUpdateGame(game)) return;
            var scorersBeforeUpdate = GetScorersByEventId(game);
            var result = await _nhlApi.GetPatchAsync(game.GamePk, game.MetaData.TimeStamp);

            if (result.Type == JTokenType.Array)
            {
                var diffs = result.ToObject<List<Diffs>>();
                var patchDocs = new List<JsonPatchDocument>();

                if (diffs != null && diffs.Count == 0 && game.GameData.Datetime.DateTime <= DateTime.UtcNow.AddHours(-5) && !game.GameData.Status.IsOver)
                {
                    game = await _nhlApi.GetGameAsync(game.GamePk);
                }
                else
                {

                    if (diffs != null)
                    {
                        foreach (var diff in diffs)
                        {
                            var patchDoc = new JsonPatchDocument();
                            patchDoc.Operations.AddRange(diff.Diff.Select(diff => new Operation<LiveGame>(diff.Op, diff.Path, diff.From, diff.Value)));
                            patchDocs.Add(patchDoc);
                        }
                    }

                    foreach (var patch in patchDocs)
                    {
                        patch.Operations.RemoveAll(o => !o.path.Contains("/liveData/plays/allPlays/") && (o.op != "replace" || o.path != "/metaData/timeStamp"));
                        foreach (var operation in patch.Operations)
                        {
                            if (operation.op == "add" && Regex.IsMatch(operation.path, @"/[0-9]*$"))
                            {
                                var oldPath = operation.path;
                                var newPath = string.Concat(operation.path.AsSpan(0, operation.path.LastIndexOf('/')), "/-");
                                operation.path = newPath;
                            }
                        }

                        patch.ApplyTo(game, (error) =>
                        {
                            _logger.LogError(error.ErrorMessage);
                            _logger.LogError(JsonConvert.SerializeObject(error.Operation));
                        });
                    }
                }
            } else if (result.Type == JTokenType.Object)
            {
                game = result.ToObject<LiveGame>();
            }

            _gameCache.UpdateGame(game);

            var scorersAfterUpdate = GetScorersByEventId(game);

            foreach (var scorerAfterUpdate in scorersAfterUpdate)
            {
                var isNewGoal = !scorersBeforeUpdate.ContainsKey(scorerAfterUpdate.Key);
                var newScoringPlay = game.LiveData.Plays.AllPlays.First(p => p.About.EventId == scorerAfterUpdate.Key);
                if (isNewGoal)
                {
                    await HandleNewScoringPlay(game.GamePk, newScoringPlay);
                }
                else
                {
                    var oldScorer = scorersBeforeUpdate[scorerAfterUpdate.Key];
                    var newScorer = scorerAfterUpdate.Value;

                    if (oldScorer.Id != newScorer.Id)
                    {
                        await HandleScorerChange(game.GamePk, newScoringPlay, newScorer, oldScorer);
                        await HandleNewScoringPlay(game.GamePk, newScoringPlay);
                    }
                }
            }

            foreach (var scorerBeforeUpdate in scorersBeforeUpdate)
            {
                var wasRemoved = !scorersAfterUpdate.ContainsKey(scorerBeforeUpdate.Key);
                if (!wasRemoved) continue;

                var scorer = scorerBeforeUpdate.Value;
                await HandleGoalRemoved(game.GamePk, scorerBeforeUpdate.Key, scorer);
            }
        }

        private static Dictionary<int, PlayerSummary> GetScorersByEventId(LiveGame game) =>
            game.LiveData.Plays.AllPlays
            .Where(play => play.Result.EventTypeId == GameEventTypes.Goal && play.Players?.FirstOrDefault(p => p.PlayerType == PlayerTypes.Scorer)?.Player.Id != null)
            .DistinctBy(play => play.About.EventId)
            .ToDictionary(k => k.About.EventId, v => new PlayerSummary()
            {
                Id = v.Players.First(p => p.PlayerType == PlayerTypes.Scorer).Player.Id,
                FullName = v.Players.First(p => p.PlayerType == PlayerTypes.Scorer).Player.FullName,
                Link = v.Players.First(p => p.PlayerType == PlayerTypes.Scorer).Player.Link
            });

        private async Task HandleNewScoringPlay(long gamePk, Play play)
        {
            var scorer = play.Players?.FirstOrDefault(p => p.PlayerType == PlayerTypes.Scorer);
            if (scorer == null) return;

            var picksToReward = await _dbContext
                .LobbyMemberPicks
                .Include(pick => pick.Drinks)
                .Include(pick => pick.LobbyMember)
                    .ThenInclude(member => member.Lobby)
                        .ThenInclude(lobby => lobby.LobbyMembers)
                .Where(pick => pick.GamePk == gamePk
                    && pick.PlayerId == scorer.Player.Id
                    && !pick.Drinks.Any(d => d.EventId == play.About.EventId))
                .ToListAsync();

            foreach (var pickToReward in picksToReward)
            {
                var drink = new Drink()
                {
                    LobbyMemberPickId = pickToReward.Id,
                    EventId = play.About.EventId
                };

                _dbContext.Drinks.Add(drink);
                await _dbContext.SaveChangesAsync();

                await _lobbyEventService.SendDrinkAwardedEvent(pickToReward.LobbyMember.Lobby, pickToReward.LobbyMember, gamePk, play.About.EventId, scorer.Player.Id, play.Team.Id);

                if (pickToReward.LobbyMember.IsBot)
                {
                    var members = pickToReward.LobbyMember.Lobby.LobbyMembers.Where(member => !member.IsBot).ToList();
                    var index = _random.Next(members.Count);
                    var recipient = members[index];

                    if (recipient != null)
                        await _lobbyService.AssignDrink(pickToReward.LobbyMember.UserId, recipient.Lobby.JoinCode, drink.Id, recipient.Id);
                }
            }
        }

        private async Task HandleScorerChange(long gamePk, Play play, PlayerSummary newScorer, PlayerSummary oldScorer)
        {
            await _lobbyEventService.SendGoalChangedEvent(gamePk, newScorer.Id, oldScorer.Id, play.Team.Id);

            var affectedDrinks = await _dbContext.Drinks
                .Include(d => d.RecipientLobbyMember)
                .Include(d => d.LobbyMemberPick)
                    .ThenInclude(lmp => lmp.LobbyMember)
                        .ThenInclude(lm => lm.Lobby)
                .Where(d => d.EventId == play.About.EventId && d.LobbyMemberPick.GamePk == gamePk)
                .ToListAsync();

            if (!affectedDrinks.Any()) return;

            foreach (var drink in affectedDrinks) {
                if (drink.RecipientLobbyMember != null)
                    await _lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gamePk, play.About.EventId, oldScorer.Id);
                else
                {
                    await _lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                    _dbContext.Drinks.Remove(drink);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleGoalRemoved(long gamePk, int eventId, PlayerSummary scorer)
        {
            return;
            await _lobbyEventService.SendGoalRemovedEvent(gamePk, scorer.Id);

            var affectedDrinks = await _dbContext.Drinks
                .Include(d => d.RecipientLobbyMember)
                .Include(d => d.LobbyMemberPick)
                    .ThenInclude(lmp => lmp.LobbyMember)
                        .ThenInclude(lm => lm.Lobby)
                .Where(d => d.EventId == eventId && d.LobbyMemberPick.GamePk == gamePk)
                .ToListAsync();

            if (!affectedDrinks.Any()) return;

            foreach (var drink in affectedDrinks)
            {
                if (drink.RecipientLobbyMember != null)
                    await _lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gamePk, eventId, scorer.Id);
                else
                {
                    await _lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                    _dbContext.Drinks.Remove(drink);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private static bool ShouldUpdateGame(LiveGame game)
        {
            if (game.GameData.Status.IsLive) return true;
            if (game.GameData.Datetime.DateTime <= DateTime.UtcNow.AddHours(1)) return true;

            return false;
        }

        private async Task AddAllScheduledGamesToCache()
        {
            var schedule = await _nhlApi.GetScheduleAsync(DateTime.UtcNow.AddHours(-10));
            if (!schedule.Dates.Any()) return;

            var date = schedule.Dates.First();
            var gamePks = date.Games
                .Where(game => !GameIsStale(game))
                .Select(game => game.GamePk)
                .ToList();
            var tasks = gamePks.Select(_nhlApi.GetGameAsync);
            var games = await Task.WhenAll(tasks);

            foreach (var game in games)
                _gameCache.AddGame(game);
        }

        private void RemoveOldGamesFromCache()
        {
            var cachedGames = _gameCache.GetAllGames();
            var staleGames = cachedGames
                .Where(GameIsStale)
                .ToList();

            staleGames.ForEach(_gameCache.RemoveGame);
        }

        private bool GameIsStale(LiveGame game)
            => game.GameData.Status.IsOver
                && game.GameData.Datetime.EndDateTime != null
                && game.GameData.Datetime.EndDateTime < DateTime.UtcNow.AddMinutes(-10);

        private bool GameIsStale(GameSummary game)
            => game.Status.IsOver;
    }
}
