using DraftPuck.Infrastructure.Application;
using DraftPuck.Infrastructure.Database;
using DraftPuck.Shared.Interfaces;
using Microsoft.Extensions.Options;

namespace DraftPuck.Core.Services;

public class GameService(INhlService nhlApi, IGameCache gameCache, DraftPuckContext dbContext, ILobbyService lobbyService, ILobbyEventService lobbyEventService, IMapper mapper, IFirebaseService firebaseService, IOptions<ApplicationOptions> appConfig) : IGameService
{
    private static readonly Random random = new();
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task CheckGamesAsync()
    {
        try
        {
            var cachedGames = gameCache.GetAllGames();
            if (cachedGames.Count == 0 || (DateTime.UtcNow.Minute == 0 && DateTime.UtcNow.Second <= 10))
            {
                await CheckScheduleAsync(cachedGames);
            }

            foreach (var game in cachedGames)
            {
                await UpdateGameAsync(game);
            }

            //check once per minute
            if (DateTime.UtcNow.Second < 10)
                await NotifyIfNewPicksAreAvailable();
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to check games: " + e.Message);
            if (e.StackTrace != null)
                Console.WriteLine(e.StackTrace);
        }
    }

    public Game GetGameById(int id)
    {
        return gameCache.GetGameById(id)!;
    }

    public List<Game> GetAllGames()
    {
        return gameCache.GetAllGames();
    }

    public List<GameSummary> GetAllGameSummaries()
    {
        var games = gameCache.GetAllGames();
        return mapper.Map<List<GameSummary>>(games);
    }

    private async Task UpdateGameAsync(Game cachedGame)
    {
        if (!ShouldUpdateGame(cachedGame))
        {
            return;
        }

        var existingHomeRoster = cachedGame.HomeTeam.Roster;
        var existingAwayRoster = cachedGame.AwayTeam.Roster;

        var updatedGame = await nhlApi.GetGameAsync(cachedGame.Id);

        if (updatedGame.PlayerSummaries.Any() && !existingHomeRoster.Any() && !existingAwayRoster.Any())
        {
            var playersWithStats = await FetchPlayerStatsAsync(updatedGame.PlayerSummaries);

            //Player Summaries Team ID represents current game (e.g. Team USA), but Player Stats Team ID represents NHL team,
            //so we need to overwrite the Team ID for each player with their current game team
            playersWithStats.ForEach(player =>
            {
                player.TeamId = updatedGame.PlayerSummaries.First(ps => ps.Id == player.Id).TeamId;
            });

            existingHomeRoster = playersWithStats.Where(player => player.TeamId == cachedGame.HomeTeam.Id).ToList();
            existingAwayRoster = playersWithStats.Where(player => player.TeamId == cachedGame.AwayTeam.Id).ToList();
        }

        updatedGame.HomeTeam.Roster = existingHomeRoster;
        updatedGame.AwayTeam.Roster = existingAwayRoster;
        cachedGame.HomeTeam.Roster = existingHomeRoster;
        cachedGame.AwayTeam.Roster = existingAwayRoster;

        SetPlayDateTimes(updatedGame, cachedGame);

        HandleTestMode(updatedGame);

        var goalsBeforeUpdate = GetGoalSummaries(cachedGame);
        var goalsAfterUpdate = GetGoalSummaries(updatedGame);

        gameCache.UpdateGame(updatedGame);

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
        var scorerId = play.PrimaryPlayerId;
        if (scorerId == null)
        {
            return;
        }

        var picksToReward = await dbContext
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
            Drink drink = new()
            {
                LobbyMemberPickId = pickToReward.Id,
                EventId = play.Id
            };

            dbContext.Drinks.Add(drink);
            await dbContext.SaveChangesAsync();

            await lobbyEventService.SendDrinkAwardedEvent(pickToReward.LobbyMember.Lobby, pickToReward.LobbyMember, gameId, play.Id, scorerId.Value, play.PrimaryTeamId!.Value);
            await HandleDrinkAwardedNotifications(pickToReward.LobbyMember.Lobby, pickToReward.LobbyMember);

            if (pickToReward.LobbyMember.IsBot)
            {
                var members = pickToReward.LobbyMember.Lobby.LobbyMembers.Where(member => !member.IsBot && !member.IsRemoved).ToList();
                var randomIndex = random.Next(members.Count);
                var recipient = members[randomIndex];

                if (recipient != null)
                {
                    await lobbyService.AssignDrink(pickToReward.LobbyMember.UserId, recipient.Lobby.JoinCode, drink.Id, recipient.Id);
                }
            }
        }
    }

    private async Task HandleDrinkAwardedNotifications(Lobby lobby, LobbyMember recipient)
    {
        if (recipient.IsBot) return; //don't send notifications when bots are awarded a drink, since they give them out immediately

        var lobbyUserIds = lobby.LobbyMembers
           .Where(lm => !lm.IsBot)
           .Select(lm => lm.UserId);

        var lobbyUsers = dbContext.Users.Where(u => lobbyUserIds.Contains(u.Id)).ToList();
        await Parallel.ForEachAsync(lobbyUsers, async (user, _) =>
        {
            if (user.FcmRegistrationToken == null || user.DrinkAwardedNotificationPreference == NotificationPreference.None) return; //notifications disabled

            var userName = lobby.LobbyMembers.Single(lm => lm.UserId == user.Id).Name;
            var isRecipient = recipient.UserId == user.Id;
            var title = LobbyEventTexts.GetTitle(LobbyEventType.DrinkAwarded);
            var text = LobbyEventTexts.GetText(LobbyEventType.DrinkAwarded).Replace("{{name}}", recipient.Name).Replace(" {{playerBadge}}", "");

            if (!isRecipient && user.DrinkAwardedNotificationPreference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", LobbyEventType.DrinkAwarded.ToString() }, { "isRelevant", "false" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, title, text, user.FcmRegistrationToken, data);
            }
            else if (isRecipient)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", LobbyEventType.DrinkAwarded.ToString() }, { "isRelevant", "true" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, "🚨 GOAL 🚨", text, user.FcmRegistrationToken, data);
            }
        });
    }

    private static Player GetPlayerById(Game game, int id)
    {
        var allPlayers = game.HomeTeam.Roster.Concat(game.AwayTeam.Roster);
        return allPlayers.Single(p => p.Id == id);
    }

    private async Task HandleScorerChangeAsync(int gameId, Play play, Player newScorer, Player oldScorer)
    {
        await lobbyEventService.SendGoalChangedEvent(gameId, newScorer.Id, oldScorer.Id, play.PrimaryTeamId!.Value);

        var affectedDrinks = await dbContext.Drinks
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

        foreach (var drink in affectedDrinks)
        {
            if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
            {
                await lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gameId, play.Id, oldScorer.Id);
            }
            else if (drink.LobbyMemberPick.IsActive)
            {
                await lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                dbContext.Drinks.Remove(drink);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task HandleGoalRemovedAsync(int gameId, int eventId, Player scorer)
    {
        var affectedDrinks = await dbContext.Drinks
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

        foreach (var drink in affectedDrinks)
        {
            if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
            {
                await lobbyEventService.SendGoalRemovedEvent(gameId, scorer.Id);
                await lobbyEventService.SendDrinkInvalidatedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember, drink.RecipientLobbyMember, gameId, eventId, scorer.Id);
            }
            else if (drink.LobbyMemberPick.IsActive)
            {
                await lobbyEventService.SendGoalRemovedEvent(gameId, scorer.Id);
                await lobbyEventService.SendDrinkRemovedEvent(drink.LobbyMemberPick.LobbyMember.Lobby, drink.LobbyMemberPick.LobbyMember);
                dbContext.Drinks.Remove(drink);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private bool ShouldUpdateGame(Game game)
    {
        return game.GameState == GameState.Live || game.DateTime <= _appConfig.CurrentTimeUtc.AddHours(1);
    }

    private async Task CheckScheduleAsync(List<Game> cachedGames)
    {
        var schedule = await nhlApi.GetScheduleByDateAsync(_appConfig.CurrentTimeUtc.AddHours(-4));

        //Remove old games
        foreach (var cachedGame in cachedGames)
        {
            if (!schedule.Games.Select(g => g.Id).Contains(cachedGame.Id))
            {
                gameCache.RemoveGame(cachedGame);
            }
        }

        if (!schedule.Games.Any())
        {
            return;
        }

        var getGamesNotYetCached = schedule.Games
            .Where(scheduledGame => !cachedGames.Select(g => g.Id).Contains(scheduledGame.Id))
            .Select(game => nhlApi.GetGameAsync(game.Id));

        (await Task.WhenAll(getGamesNotYetCached))
            .ToList()
            .ForEach(game =>
            {
                SetPlayDateTimes(game);
                HandleTestMode(game);
                gameCache.AddGame(game);
            });
    }

    private async Task<List<Player>> FetchPlayerStatsAsync(List<PlayerSummary> playerSummaries)
    {
        List<Task<Player>> playerTasks = new();
        foreach (var playerSummary in playerSummaries)
        {
            playerTasks.Add(nhlApi.GetPlayerAsync(playerSummary.Id));
        }

        await Task.WhenAll(playerTasks);
        return playerTasks
            .Select(task => task.Result)
            .OrderByDescending(p => p.Goals)
            .ToList();
    }

    private static void SetPlayDateTimes(Game updatedGame, Game? cachedGame = null)
    {
        var FULL_PERIOD_DURATION = 40;
        var SHORT_PERIOD_DURATION = 7;
        var FULL_INTERMISSION_DURATION = 22;
        var SHORT_INTERMISSION_DURATION = 2;

        foreach (var play in updatedGame.Plays)
        {
            if (cachedGame != null)
            {
                var previousPlay = cachedGame.Plays.SingleOrDefault(p => p.Id == play.Id);

                if (previousPlay != null && previousPlay.DateTime != DateTime.MinValue)
                {
                    play.DateTime = previousPlay.DateTime;
                    continue;
                }
            }

            var fullPeriodsCompleted = 0;
            var fullIntermissionsCompleted = 0;
            var shortPeriodsCompleted = 0;
            var shortIntermissionsCompleted = 0;

            if (updatedGame.GameType == GameType.Playoffs)
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
            var timeWithOffset = updatedGame.DateTime.Add(offset);
            play.DateTime = timeWithOffset > DateTime.UtcNow ? DateTime.UtcNow : timeWithOffset;
        }
    }

    private void HandleTestMode(Game game)
    {
        if (!_appConfig.IsTestMode) return;

        //Plays
        game.Plays = game.Plays.Where(play => play.DateTime <= _appConfig.CurrentTimeUtc).ToList();

        //Game State
        var gameStarted = _appConfig.CurrentTimeUtc >= game.DateTime;
        var gameEnded = game.Plays.Any(play => play.Type == PlayType.GameEnd);
        if (!gameStarted) game.GameState = GameState.Upcoming;
        else if (!gameEnded) game.GameState = GameState.Live;
        else game.GameState = GameState.Final;

        //Period/time
        var mostRecentPlay = game.Plays.LastOrDefault();

        game.MinutesRemainingInPeriod = mostRecentPlay != null ? MapMinutesRemaining(mostRecentPlay.TimeRemainingInPeriod) : 20;
        game.SecondsRemainingInPeriod = mostRecentPlay != null ? MapSecondsRemaining(mostRecentPlay.TimeRemainingInPeriod) : 0;

        if (game.GameState != GameState.Final)
        {
            game.Period = mostRecentPlay?.Period ?? 1;
            game.PeriodType = mostRecentPlay?.PeriodType ?? PeriodType.Regulation;
        }

        //Score
        game.HomeTeam.Score = game.Plays.Count(play => play.Type == PlayType.Goal && play.PrimaryTeamId == game.HomeTeam.Id);
        game.AwayTeam.Score = game.Plays.Count(play => play.Type == PlayType.Goal && play.PrimaryTeamId == game.AwayTeam.Id);

        //Goals by period
        game.GoalsByPeriod = Enumerable.Range(0, game.Period).Select(n => new PeriodSummary()
        {
            AwayGoals = game.Plays.Count(play => play.Period == n + 1 && play.Type == PlayType.Goal && play.PrimaryTeamId == game.AwayTeam.Id),
            HomeGoals = game.Plays.Count(play => play.Period == n + 1 && play.Type == PlayType.Goal && play.PrimaryTeamId == game.HomeTeam.Id),
            Number = n + 1,
            PeriodType = (n + 1) <= 3 ? PeriodType.Regulation : PeriodType.Overtime
        }).ToList();
    }

    private async Task HandleScoringUpdatesAsync(Dictionary<int, GoalSummary> goalsBeforeUpdate, Dictionary<int, GoalSummary> goalsAfterUpdate, Game game)
    {
        foreach (var goalAfterUpdate in goalsAfterUpdate)
        {
            var goalDidntChange = goalsBeforeUpdate.Any(kvp => kvp.Value.IsSameGoal(goalAfterUpdate.Value));
            if (goalDidntChange)
            {
                continue;
            }

            var isNewGoal = !goalsBeforeUpdate.ContainsKey(goalAfterUpdate.Key);
            var newScoringPlay = game.Plays.First(p => p.Id == goalAfterUpdate.Key);

            if (isNewGoal)
            {
                await HandleNewScoringPlayAsync(game.Id, newScoringPlay);
                continue;
            }

            var oldScorer = goalsBeforeUpdate[goalAfterUpdate.Key].Player;
            var newScorer = goalAfterUpdate.Value.Player;
            var isScoringChange = oldScorer.Id != newScorer.Id;

            if (isScoringChange)
            {
                await HandleScorerChangeAsync(game.Id, newScoringPlay, newScorer, oldScorer);
                await HandleNewScoringPlayAsync(game.Id, newScoringPlay);
                continue;
            }
        }

        foreach (var goalBeforeUpdate in goalsBeforeUpdate)
        {
            var goalDidntChange = goalsAfterUpdate.Any(kvp => kvp.Value.IsSameGoal(goalBeforeUpdate.Value));
            if (goalDidntChange)
            {
                return;
            }

            var wasRemoved = !goalsAfterUpdate.ContainsKey(goalBeforeUpdate.Key);
            if (!wasRemoved)
            {
                continue;
            }

            var scorer = goalBeforeUpdate.Value.Player;
            await HandleGoalRemovedAsync(game.Id, goalBeforeUpdate.Key, scorer);
        }
    }

    private async Task NotifyIfNewPicksAreAvailable()
    {
        var PICK_TIME_MINUTES_BEFORE_GAME = 30;
        var cachedGames = gameCache.GetAllGames();
        var gamesToSendNotificationsFor = cachedGames.Where(game =>
        {
            var timeDiff = game.DateTime - DateTime.UtcNow;
            return timeDiff.TotalMinutes < PICK_TIME_MINUTES_BEFORE_GAME && timeDiff.TotalMinutes > (PICK_TIME_MINUTES_BEFORE_GAME - 1);
        });

        if (!gamesToSendNotificationsFor.Any()) return;

        var currentActiveLobbies = await lobbyService.GetAllLobbies();
        var lobbiesWithRelevantGames = currentActiveLobbies.Where(lobby => lobby.GameIds.Any(gameId => gamesToSendNotificationsFor.Select(game => game.Id).Contains(gameId)));
        var lobbyMembers = lobbiesWithRelevantGames.SelectMany(lobby => lobby.LobbyMembers);
        var lobbyUserIds = lobbyMembers.Select(lm => lm.UserId);
        var lobbyUsers = await dbContext.Users.Where(u => lobbyUserIds.Contains(u.Id) && u.PickingStartedNotificationPreference != NotificationPreference.None && u.FcmRegistrationToken != null).ToListAsync();

        var usersByLobby = lobbiesWithRelevantGames.ToDictionary(l => l.JoinCode, l => lobbyUsers.Where(u => l.LobbyMembers.Select(lm => lm.UserId).Contains(u.Id)));

        foreach (var kvp in usersByLobby)
            foreach (var user in kvp.Value)
                await firebaseService.SendPushNotification(kvp.Key, "New picks available!", "30 minutes until gametime.", user.FcmRegistrationToken!);
    }

    public static PeriodType MapPeriodType(string periodType)
    {
        return periodType == "OT" ? PeriodType.Overtime : periodType == "SO" ? PeriodType.Shootout : PeriodType.Regulation;
    }

    public static int MapMinutesRemaining(string timeRemaining)
    {
        if (!timeRemaining.Contains(':'))
        {
            return 0;
        }

        var split = timeRemaining.Split(':');
        return split.Length != 2 ? 0 : int.Parse(split[0]);
    }

    public static int MapSecondsRemaining(string timeRemaining)
    {
        if (!timeRemaining.Contains(':'))
        {
            return 0;
        }

        var split = timeRemaining.Split(':');
        return split.Length != 2 ? 0 : int.Parse(split[1]);
    }
}
