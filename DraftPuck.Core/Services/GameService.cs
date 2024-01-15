using DraftPuck.Data.Data;

namespace DraftPuck.Core.Services;

public class GameService : IGameService
{
    private readonly INhlService _nhlService;
    private readonly IGameCache _gameCache;
    private readonly DraftPuckContext _dbContext;
    private readonly ILobbyService _lobbyService;
    private readonly ILobbyEventService _lobbyEventService;
    private readonly IMapper _mapper;
    private static readonly Random _random = new();

    public GameService(INhlService nhlApi, IGameCache gameCache, DraftPuckContext dbContext, ILobbyService lobbyService, ILobbyEventService lobbyEventService, IMapper mapper)
    {
        _nhlService = nhlApi;
        _gameCache = gameCache;
        _dbContext = dbContext;
        _lobbyService = lobbyService;
        _lobbyEventService = lobbyEventService;
        _mapper = mapper;
    }

    public async Task CheckGamesAsync()
    {
        List<Game> cachedGames = _gameCache.GetAllGames();
        if (cachedGames.Count == 0 || (DateTime.UtcNow.Minute == 0 && DateTime.UtcNow.Second <= 10))
        {
            await CheckScheduleAsync(cachedGames);
            return;
        }

        foreach (Game game in cachedGames)
        {
            await UpdateGameAsync(game);
        }
    }

    public Game GetGameById(int id)
    {
        return _gameCache.GetGameById(id)!;
    }

    public List<Game> GetAllGames()
    {
        return _gameCache.GetAllGames();
    }

    public List<GameSummary> GetAllGameSummaries()
    {
        List<Game> games = _gameCache.GetAllGames();
        return _mapper.Map<List<GameSummary>>(games);
    }

    private async Task UpdateGameAsync(Game cachedGame)
    {
        if (!ShouldUpdateGame(cachedGame))
        {
            return;
        }

        List<Player> existingHomeRoster = cachedGame.HomeTeam.Roster;
        List<Player> existingAwayRoster = cachedGame.AwayTeam.Roster;

        Game updatedGame = await _nhlService.GetGameAsync(cachedGame.Id);

        if (updatedGame.PlayerSummaries.Any() && !existingHomeRoster.Any() && !existingAwayRoster.Any())
        {
            List<Player> playersWithStats = await FetchPlayerStatsAsync(updatedGame.PlayerSummaries);
            existingHomeRoster = playersWithStats.Where(player => player.TeamId == cachedGame.HomeTeam.Id).ToList();
            existingAwayRoster = playersWithStats.Where(player => player.TeamId == cachedGame.AwayTeam.Id).ToList();
        }

        updatedGame.HomeTeam.Roster = existingHomeRoster;
        updatedGame.AwayTeam.Roster = existingAwayRoster;
        cachedGame.HomeTeam.Roster = existingHomeRoster;
        cachedGame.AwayTeam.Roster = existingAwayRoster;

        SetPlayDateTimes(cachedGame, updatedGame);

        Dictionary<int, GoalSummary> goalsBeforeUpdate = GetGoalSummaries(cachedGame);
        Dictionary<int, GoalSummary> goalsAfterUpdate = GetGoalSummaries(updatedGame);

        _gameCache.UpdateGame(updatedGame);

        await HandleScoringUpdatesAsync(goalsBeforeUpdate, goalsAfterUpdate, updatedGame);
    }

    private static Dictionary<int, GoalSummary> GetGoalSummaries(Game game)
    {
        return game.Plays
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
    }

    private async Task HandleNewScoringPlayAsync(int gameId, Play play)
    {
        int? scorerId = play.PrimaryPlayerId;
        if (scorerId == null)
        {
            return;
        }

        List<LobbyMemberPick> picksToReward = await _dbContext
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

        foreach (LobbyMemberPick? pickToReward in picksToReward)
        {
            Drink drink = new()
            {
                LobbyMemberPickId = pickToReward.Id,
                EventId = play.Id
            };

            _ = _dbContext.Drinks.Add(drink);
            _ = await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendDrinkAwardedEvent(pickToReward.LobbyMember.Lobby, pickToReward.LobbyMember, gameId, play.Id, scorerId.Value, play.PrimaryTeamId!.Value);

            if (pickToReward.LobbyMember.IsBot)
            {
                List<LobbyMember> members = pickToReward.LobbyMember.Lobby.LobbyMembers.Where(member => !member.IsBot && !member.IsRemoved).ToList();
                int randomIndex = _random.Next(members.Count);
                LobbyMember recipient = members[randomIndex];

                if (recipient != null)
                {
                    _ = await _lobbyService.AssignDrink(pickToReward.LobbyMember.UserId, recipient.Lobby.JoinCode, drink.Id, recipient.Id);
                }
            }
        }
    }

    private static Player GetPlayerById(Game game, int id)
    {
        IEnumerable<Player> allPlayers = game.HomeTeam.Roster.Concat(game.AwayTeam.Roster);
        return allPlayers.Single(p => p.Id == id);
    }

    private async Task HandleScorerChangeAsync(int gameId, Play play, Player newScorer, Player oldScorer)
    {
        await _lobbyEventService.SendGoalChangedEvent(gameId, newScorer.Id, oldScorer.Id, play.PrimaryTeamId!.Value);

        List<Drink> affectedDrinks = await _dbContext.Drinks
            .Include(d => d.RecipientLobbyMember)
            .Include(d => d.LobbyMemberPick)
                .ThenInclude(lmp => lmp.LobbyMember)
                    .ThenInclude(lm => lm.Lobby)
            .Where(d => d.EventId == play.Id && d.LobbyMemberPick.GameId == gameId)
            .ToListAsync();

        if (!affectedDrinks.Any())
        {
            return;
        }

        foreach (Drink? drink in affectedDrinks)
        {
            if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
            {
                await _lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gameId, play.Id, oldScorer.Id);
            }
            else if (drink.LobbyMemberPick.IsActive)
            {
                await _lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                _ = _dbContext.Drinks.Remove(drink);
            }
        }

        _ = await _dbContext.SaveChangesAsync();
    }

    private async Task HandleGoalRemovedAsync(int gameId, int eventId, Player scorer)
    {
        List<Drink> affectedDrinks = await _dbContext.Drinks
            .Include(d => d.RecipientLobbyMember)
            .Include(d => d.LobbyMemberPick)
                .ThenInclude(lmp => lmp.LobbyMember)
                    .ThenInclude(lm => lm.Lobby)
            .Where(d => d.EventId == eventId && d.LobbyMemberPick.GameId == gameId)
            .ToListAsync();

        if (!affectedDrinks.Any())
        {
            return;
        }

        foreach (Drink? drink in affectedDrinks)
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
                _ = _dbContext.Drinks.Remove(drink);
            }
        }

        _ = await _dbContext.SaveChangesAsync();
    }

    private static bool ShouldUpdateGame(Game game)
    {
        return game.GameState == GameState.Live || game.DateTime <= DateTime.UtcNow.AddHours(1);
    }

    private async Task CheckScheduleAsync(List<Game> cachedGames)
    {
        Schedule schedule = await _nhlService.GetScheduleByDateAsync(DateTime.UtcNow.AddHours(-4));

        //Remove old games
        foreach (Game cachedGame in cachedGames)
        {
            if (!schedule.Games.Select(g => g.Id).Contains(cachedGame.Id))
            {
                _gameCache.RemoveGame(cachedGame);
            }
        }

        if (!schedule.Games.Any())
        {
            return;
        }

        IEnumerable<Task<Game>> getGamesNotYetCached = schedule.Games
            .Where(scheduledGame => !cachedGames.Select(g => g.Id).Contains(scheduledGame.Id))
            .Select(game => _nhlService.GetGameAsync(game.Id));

        (await Task.WhenAll(getGamesNotYetCached))
            .ToList()
            .ForEach(_gameCache.AddGame);
    }

    private async Task<List<Player>> FetchPlayerStatsAsync(List<PlayerSummary> playerSummaries)
    {
        List<Task<Player>> playerTasks = new();
        foreach (PlayerSummary playerSummary in playerSummaries)
        {
            playerTasks.Add(_nhlService.GetPlayerAsync(playerSummary.Id));
        }

        _ = await Task.WhenAll(playerTasks);
        return playerTasks
            .Select(task => task.Result)
            .OrderByDescending(p => p.Goals)
            .ToList();
    }

    private static void SetPlayDateTimes(Game cachedGame, Game updatedGame)
    {
        int FULL_PERIOD_DURATION = 40;
        int SHORT_PERIOD_DURATION = 7;
        int FULL_INTERMISSION_DURATION = 22;
        int SHORT_INTERMISSION_DURATION = 2;

        foreach (Play play in updatedGame.Plays)
        {
            Play? previousPlay = cachedGame.Plays.SingleOrDefault(p => p.Id == play.Id);
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

            int fullPeriodsCompleted = 0;
            int fullIntermissionsCompleted = 0;
            int shortPeriodsCompleted = 0;
            int shortIntermissionsCompleted = 0;

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

            TimeSpan latePuckDropModifier = TimeSpan.FromMinutes(10);
            TimeSpan periodDurations = TimeSpan.FromMinutes((fullPeriodsCompleted * FULL_PERIOD_DURATION) + (shortPeriodsCompleted * SHORT_PERIOD_DURATION));
            TimeSpan intermissionDurations = TimeSpan.FromMinutes((fullIntermissionsCompleted * FULL_INTERMISSION_DURATION) + (shortIntermissionsCompleted * SHORT_INTERMISSION_DURATION));
            List<int> periodParts = play.TimeInPeriod.Split(':').Select(int.Parse).ToList();
            TimeSpan periodDuration = TimeSpan.FromMinutes(periodParts[0]) + TimeSpan.FromSeconds(periodParts[1]);

            TimeSpan offset = latePuckDropModifier + periodDurations + intermissionDurations + periodDuration;
            DateTime timeWithOffset = updatedGame.DateTime.Add(offset);
            play.DateTime = timeWithOffset > DateTime.UtcNow ? DateTime.UtcNow : timeWithOffset;
        }
    }

    private async Task HandleScoringUpdatesAsync(Dictionary<int, GoalSummary> goalsBeforeUpdate, Dictionary<int, GoalSummary> goalsAfterUpdate, Game game)
    {
        foreach (KeyValuePair<int, GoalSummary> goalAfterUpdate in goalsAfterUpdate)
        {
            bool goalDidntChange = goalsBeforeUpdate.Any(kvp => kvp.Value.IsSameGoal(goalAfterUpdate.Value));
            if (goalDidntChange)
            {
                continue;
            }

            bool isNewGoal = !goalsBeforeUpdate.ContainsKey(goalAfterUpdate.Key);
            Play newScoringPlay = game.Plays.First(p => p.Id == goalAfterUpdate.Key);

            if (isNewGoal)
            {
                await HandleNewScoringPlayAsync(game.Id, newScoringPlay);
                continue;
            }

            Player oldScorer = goalsBeforeUpdate[goalAfterUpdate.Key].Player;
            Player newScorer = goalAfterUpdate.Value.Player;
            bool isScoringChange = oldScorer.Id != newScorer.Id;

            if (isScoringChange)
            {
                await HandleScorerChangeAsync(game.Id, newScoringPlay, newScorer, oldScorer);
                await HandleNewScoringPlayAsync(game.Id, newScoringPlay);
                continue;
            }
        }

        foreach (KeyValuePair<int, GoalSummary> goalBeforeUpdate in goalsBeforeUpdate)
        {
            bool goalDidntChange = goalsAfterUpdate.Any(kvp => kvp.Value.IsSameGoal(goalBeforeUpdate.Value));
            if (goalDidntChange)
            {
                return;
            }

            bool wasRemoved = !goalsAfterUpdate.ContainsKey(goalBeforeUpdate.Key);
            if (!wasRemoved)
            {
                continue;
            }

            Player scorer = goalBeforeUpdate.Value.Player;
            await HandleGoalRemovedAsync(game.Id, goalBeforeUpdate.Key, scorer);
        }
    }
}
