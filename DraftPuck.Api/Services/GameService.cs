using Draftpuck.Nhl.Services.Interfaces;

namespace DraftPuck.Api.Services
{
    public class GameService : IGameService
    {
        private readonly INhlService _nhlService;
        private readonly IGameCache _gameCache;
        private readonly DraftPuckContext _dbContext;
        private readonly ILobbyService _lobbyService;
        private readonly ILobbyEventService _lobbyEventService;
        static readonly Random _random = new();

        public GameService(INhlService nhlApi, IGameCache gameCache, DraftPuckContext dbContext, ILobbyService lobbyService, ILobbyEventService lobbyEventService)
        {
            _nhlService = nhlApi;
            _gameCache = gameCache;
            _dbContext = dbContext;
            _lobbyService = lobbyService;
            _lobbyEventService = lobbyEventService;
        }

        public async Task CheckGamesAsync()
        {
            RemoveOldGamesFromCache();

            var cachedGames = _gameCache.GetAllGames();
            if (cachedGames.Count == 0)
            {
                await AddAllScheduledGamesToCache();
                return;
            }

            foreach (var game in cachedGames)
                await UpdateGame(game);
        }

        public Game GetGameById(int id)
        {
            return _gameCache.GetGameById(id)!;
        }

        private async Task UpdateGame(Game cachedGame)
        {
            if (!ShouldUpdateGame(cachedGame)) return;
            var existingHomeRoster = cachedGame.HomeTeam.Roster;
            var existingAwayRoster = cachedGame.AwayTeam.Roster;

            var updatedGame = await _nhlService.GetGameAsync(cachedGame.Id);

            if (updatedGame.PlayerSummaries.Any() && !existingHomeRoster.Any() && !existingAwayRoster.Any())
            {
                var playersWithStats = await FetchPlayerStats(updatedGame.PlayerSummaries);
                existingHomeRoster = playersWithStats.Where(player => player.TeamId == cachedGame.HomeTeam.Id).ToList();
                existingAwayRoster = playersWithStats.Where(player => player.TeamId == cachedGame.AwayTeam.Id).ToList();
            }

            updatedGame.HomeTeam.Roster = existingHomeRoster;
            updatedGame.AwayTeam.Roster = existingAwayRoster;

            SetPlayDateTimes(cachedGame, updatedGame);

            var goalsBeforeUpdate = GetGoalSummaries(cachedGame);
            var goalsAfterUpdate = GetGoalSummaries(updatedGame);

            _gameCache.UpdateGame(updatedGame);

            await HandleScoringUpdates(goalsBeforeUpdate, goalsAfterUpdate, updatedGame);
        }

        private static Dictionary<int, GoalSummary> GetGoalSummaries(Game game) =>
            game.Plays
            .Where(play =>
                play.Type == PlayType.Goal
                && play.PrimaryPlayerId != null
                && play.PrimaryTeamId != null
                && play.PeriodType is PeriodType.Regulation or PeriodType.Overtime)
            .DistinctBy(play => play.Id)
            .ToDictionary(k => k.Id, v => new GoalSummary()
            {
                Player = GetPlayerById(game, v.PrimaryPlayerId!.Value),
                PeriodTime = v.TimeInPeriod
            });

        private async Task HandleNewScoringPlay(int gameId, Play play)
        {
            var scorerId = play.PrimaryPlayerId;
            if (scorerId == null) return;

            var picksToReward = await _dbContext
                .LobbyMemberPicks
                .Include(pick => pick.Drinks)
                .Include(pick => pick.LobbyMember)
                    .ThenInclude(member => member.Lobby)
                        .ThenInclude(lobby => lobby.LobbyMembers)
                .Where(pick => pick.IsActive
                    && pick.GameId == gameId
                    && pick.PlayerId == scorerId
                    && !pick.Drinks.Any(d => d.EventId == play.Id))
                .ToListAsync();

            foreach (var pickToReward in picksToReward)
            {
                var drink = new Drink()
                {
                    LobbyMemberPickId = pickToReward.Id,
                    EventId = play.Id
                };

                _dbContext.Drinks.Add(drink);
                await _dbContext.SaveChangesAsync();

                await _lobbyEventService.SendDrinkAwardedEvent(pickToReward.LobbyMember.Lobby, pickToReward.LobbyMember, gameId, play.Id, scorerId.Value, play.PrimaryTeamId!.Value);

                if (pickToReward.LobbyMember.IsBot)
                {
                    var members = pickToReward.LobbyMember.Lobby.LobbyMembers.Where(member => !member.IsBot && !member.IsRemoved).ToList();
                    var randomIndex = _random.Next(members.Count);
                    var recipient = members[randomIndex];

                    if (recipient != null)
                        await _lobbyService.AssignDrink(pickToReward.LobbyMember.UserId, recipient.Lobby.JoinCode, drink.Id, recipient.Id);
                }
            }
        }

        private static Player GetPlayerById(Game game, int id)
        {
            var allPlayers = game.HomeTeam.Roster.Concat(game.AwayTeam.Roster);
            return allPlayers.Single(p => p.Id == id);
        }

        private async Task HandleScorerChange(int gameId, Play play, Player newScorer, Player oldScorer)
        {
            await _lobbyEventService.SendGoalChangedEvent(gameId, newScorer.Id, oldScorer.Id, play.PrimaryTeamId!.Value);

            var affectedDrinks = await _dbContext.Drinks
                .Include(d => d.RecipientLobbyMember)
                .Include(d => d.LobbyMemberPick)
                    .ThenInclude(lmp => lmp.LobbyMember)
                        .ThenInclude(lm => lm.Lobby)
                .Where(d => d.EventId == play.Id && d.LobbyMemberPick.GameId == gameId)
                .ToListAsync();

            if (!affectedDrinks.Any()) return;

            foreach (var drink in affectedDrinks)
            {
                if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
                    await _lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gameId, play.Id, oldScorer.Id);
                else if (drink.LobbyMemberPick.IsActive)
                {
                    await _lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                    _dbContext.Drinks.Remove(drink);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleGoalRemoved(int gameId, int eventId, Player scorer)
        {
            var affectedDrinks = await _dbContext.Drinks
                .Include(d => d.RecipientLobbyMember)
                .Include(d => d.LobbyMemberPick)
                    .ThenInclude(lmp => lmp.LobbyMember)
                        .ThenInclude(lm => lm.Lobby)
                .Where(d => d.EventId == eventId && d.LobbyMemberPick.GameId == gameId)
                .ToListAsync();

            if (!affectedDrinks.Any()) return;

            foreach (var drink in affectedDrinks)
            {
                if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
                {
                    await _lobbyEventService.SendGoalRemovedEvent(gameId, scorer.Id);
                    await _lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gameId, eventId, scorer.Id);
                }
                else if (drink.LobbyMemberPick.IsActive)
                {
                    await _lobbyEventService.SendGoalRemovedEvent(gameId, scorer.Id);
                    await _lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                    _dbContext.Drinks.Remove(drink);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private static bool ShouldUpdateGame(Game game)
        {
            if (game.GameState == GameState.Live) return true;
            if (game.DateTime <= DateTime.UtcNow.AddHours(1)) return true;

            return false;
        }

        private async Task AddAllScheduledGamesToCache()
        {
            var schedule = await _nhlService.GetScheduleByDateAsync(DateTime.UtcNow.AddHours(-4));

            if (!schedule.Games.Any()) return;

            var tasks = schedule.Games
                .Where(game => !GameIsStale(game))
                .Select(game => _nhlService.GetGameAsync(game.Id));

            (await Task.WhenAll(tasks))
                .ToList()
                .ForEach(_gameCache.AddGame);
        }

        private async Task<List<Player>> FetchPlayerStats(List<PlayerSummary> playerSummaries)
        {
            var playerTasks = new List<Task<Player>>();
            foreach (var playerSummary in playerSummaries)
                playerTasks.Add(_nhlService.GetPlayerAsync(playerSummary.Id));

            await Task.WhenAll(playerTasks);
            return playerTasks.Select(task => task.Result).ToList();
        }
        private void RemoveOldGamesFromCache()
        {
            var cachedGames = _gameCache.GetAllGames();
            var staleGames = cachedGames
                .Where(GameIsStale)
                .ToList();

            staleGames.ForEach(_gameCache.RemoveGame);
        }

        private static void SetPlayDateTimes(Game cachedGame, Game updatedGame)
        {
            var FULL_PERIOD_DURATION = 40;
            var SHORT_PERIOD_DURATION = 7;
            var FULL_INTERMISSION_DURATION = 22;
            var SHORT_INTERMISSION_DURATION = 2;

            foreach (var play in updatedGame.Plays)
            {
                var previousPlay = cachedGame.Plays.SingleOrDefault(p => p.Id == play.Id);
                if (previousPlay == null)
                {
                    play.DateTime = DateTime.UtcNow;
                    continue;
                }

                if (previousPlay.DateTime != DateTime.MinValue)
                {
                    play.DateTime = previousPlay.DateTime;
                    continue;
                }

                var fullPeriodsCompleted = 0;
                var fullIntermissionsCompleted = 0;
                var shortPeriodsCompleted = 0;
                var shortIntermissionsCompleted = 0;

                if (cachedGame.GameType == GameType.Playoffs)
                {
                    fullPeriodsCompleted = play.Period - 1;
                    fullIntermissionsCompleted = fullPeriodsCompleted;
                }
                else
                {
                    fullPeriodsCompleted = Math.Min(play.Period - 1, 3);
                    shortPeriodsCompleted = Math.Max(play.Period - 3, 0);
                    fullIntermissionsCompleted = Math.Min(fullPeriodsCompleted, 2);
                    shortIntermissionsCompleted = Math.Max(play.Period - 3, 0);
                }

                var latePuckDropModifier = TimeSpan.FromMinutes(10);
                var periodDurations = TimeSpan.FromMinutes((fullPeriodsCompleted * FULL_PERIOD_DURATION) + (shortPeriodsCompleted * SHORT_PERIOD_DURATION));
                var intermissionDurations = TimeSpan.FromMinutes((fullIntermissionsCompleted * FULL_INTERMISSION_DURATION) + (shortIntermissionsCompleted * SHORT_INTERMISSION_DURATION));
                var periodParts = play.TimeInPeriod.Split(':').Select(int.Parse).ToList();
                var periodDuration = TimeSpan.FromMinutes(periodParts[0]) + TimeSpan.FromSeconds(periodParts[1]);

                var offset = latePuckDropModifier + periodDurations + intermissionDurations + periodDuration;

                play.DateTime = updatedGame.DateTime.Add(offset);
            }
        }

        private async Task HandleScoringUpdates(Dictionary<int, GoalSummary> goalsBeforeUpdate, Dictionary<int, GoalSummary> goalsAfterUpdate, Game game)
        {
            foreach (var goalAfterUpdate in goalsAfterUpdate)
            {
                var goalDidntChange = goalsBeforeUpdate.Any(kvp => kvp.Value.IsSameGoal(goalAfterUpdate.Value));
                if (goalDidntChange) continue;

                var isNewGoal = !goalsBeforeUpdate.ContainsKey(goalAfterUpdate.Key);
                var newScoringPlay = game.Plays.First(p => p.Id == goalAfterUpdate.Key);

                if (isNewGoal)
                {
                    await HandleNewScoringPlay(game.Id, newScoringPlay);
                    continue;
                }

                var oldScorer = goalsBeforeUpdate[goalAfterUpdate.Key].Player;
                var newScorer = goalAfterUpdate.Value.Player;
                var isScoringChange = oldScorer.Id != newScorer.Id;

                if (isScoringChange)
                {
                    await HandleScorerChange(game.Id, newScoringPlay, newScorer, oldScorer);
                    await HandleNewScoringPlay(game.Id, newScoringPlay);
                    continue;
                }
            }

            foreach (var goalBeforeUpdate in goalsBeforeUpdate)
            {
                var goalDidntChange = goalsAfterUpdate.Any(kvp => kvp.Value.IsSameGoal(goalBeforeUpdate.Value));
                if (goalDidntChange) return;

                var wasRemoved = !goalsAfterUpdate.ContainsKey(goalBeforeUpdate.Key);
                if (!wasRemoved) continue;

                var scorer = goalBeforeUpdate.Value.Player;
                await HandleGoalRemoved(game.Id, goalBeforeUpdate.Key, scorer);
            }
        }

        private static bool GameIsStale(Game game) => game.GameState == GameState.Final;
        private static bool GameIsStale(GameSummary game) => game.GameState == GameState.Final;
    }
}
