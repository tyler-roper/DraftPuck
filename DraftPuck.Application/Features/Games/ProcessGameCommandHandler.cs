using DraftPuck.Application.Features.Lobbies.Picks;
using Microsoft.Extensions.Options;
using System.Text;

namespace DraftPuck.Application.Features.Games;

public class ProcessGameCommandHandler(IGameCache gameCache, IMediator mediator, IDbContext dbContext, INhlClient nhlClient, INhlQueueService nhlQueue, IOptions<ApplicationOptions> appConfig, ILogger<ProcessGameCommandHandler> logger) : IRequestHandler<ProcessGameCommand>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(ProcessGameCommand request, CancellationToken ct)
    {
        TimeSpan fallbackDelay = TimeSpan.FromMinutes(2);

        try
        {
            await HandleInternal(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing game {gameId}. Scheduling fallback retry.",
                request.GameId);

            await nhlQueue.SendMessageAsync(
                new ProcessGameMessage(request.GameId, false),
                fallbackDelay,
                ct
            );
        }
    }

    private async Task HandleInternal(ProcessGameCommand request, CancellationToken ct)
    {
        var updatedGame = await nhlClient.GetFullGameAsync(request.GameId);
        if (updatedGame == null) return;

        var nextRun = await gameCache.GetNextRunAsync(request.GameId);
        if (nextRun.HasValue && nextRun > DateTime.UtcNow)
        {
            logger.LogInformation(
                "Skipping {awayTeam} @ {homeTeam} because another process is already scheduled at {nextRun}.",
                updatedGame.AwayTeam.Abbreviation,
                updatedGame.HomeTeam.Abbreviation,
                nextRun.Value
            );
            return;
        }

        if (!GameProcessingHelpers.IsGameCurrent(updatedGame, _appConfig.CurrentTimeUtc))
        {
            await gameCache.RemoveGameAsync(updatedGame.Id);
            logger.LogInformation("{awayTeam} @ {homeTeam} has been removed from the cache.", updatedGame.AwayTeam.Abbreviation, updatedGame.HomeTeam.Abbreviation);
            return;
        }

        logger.LogInformation("Processing {awayTeam} @ {homeTeam}...", updatedGame.AwayTeam.Abbreviation, updatedGame.HomeTeam.Abbreviation);

        var cachedGame = await gameCache.GetGameByIdAsync(request.GameId);
        if (cachedGame == null)
        {
            await gameCache.AddGameAsync(updatedGame);
            cachedGame = updatedGame;
        }

        var updatedHomeSummaries = updatedGame.PlayerSummaries
            .Where(ps => ps.TeamId == cachedGame.HomeTeam.Id)
            .ToList();

        var updatedAwaySummaries = updatedGame.PlayerSummaries
            .Where(ps => ps.TeamId == cachedGame.AwayTeam.Id)
            .ToList();

        await HandleRemovedPlayersAsync(cachedGame, updatedGame, updatedHomeSummaries, updatedAwaySummaries, ct);

        var homeRoster = cachedGame.HomeTeam.Roster;
        var awayRoster = cachedGame.AwayTeam.Roster;

        if (!request.IsInitialPopulation) //skip blocking the api queue with player requests until we've got the latest copy of each game
        {
            var oldHomePlayerIds = homeRoster.Select(p => p.Id).ToHashSet();
            var updatedHomePlayerIds = updatedHomeSummaries.Select(s => s.Id).ToHashSet();

            bool homeRosterChanged = !oldHomePlayerIds.SetEquals(updatedHomePlayerIds);

            var oldAwayPlayerIds = awayRoster.Select(p => p.Id).ToHashSet();
            var updatedAwayPlayerIds = updatedAwaySummaries.Select(s => s.Id).ToHashSet();

            bool awayRosterChanged = !oldAwayPlayerIds.SetEquals(updatedAwayPlayerIds);

            if (homeRosterChanged)
            {
                var playerDtos = await FetchPlayerDetails(updatedHomeSummaries);
                playerDtos.ForEach(p => p.TeamId = cachedGame.HomeTeam.Id); // Overwrite player's usual team with their current team (e.g., they play for LAK but this is a USA vs Canada game)
                homeRoster = playerDtos;
            }

            if (awayRosterChanged)
            {
                var playersDto = await FetchPlayerDetails(updatedAwaySummaries);
                playersDto.ForEach(p => p.TeamId = cachedGame.AwayTeam.Id);
                awayRoster = playersDto;
            }
        }

        cachedGame.HomeTeam.Roster = homeRoster;
        cachedGame.AwayTeam.Roster = awayRoster;
        updatedGame.HomeTeam.Roster = [.. homeRoster.OrderByDescending(p => p.Goals)];
        updatedGame.AwayTeam.Roster = [.. awayRoster.OrderByDescending(p => p.Goals)];

        GameProcessingHelpers.SetPlayDateTimes(updatedGame, _appConfig.CurrentTimeUtc, cachedGame);
        GameProcessingHelpers.HandleTestMode(updatedGame, _appConfig);

        var goalsBefore = GetGoalSummaries(cachedGame);
        var goalsAfter = GetGoalSummaries(updatedGame);

        await gameCache.UpdateGameAsync(updatedGame);
        await HandleScoringUpdatesAsync(goalsBefore, goalsAfter, updatedGame, ct);

        var pollAgainDelay = request.IsInitialPopulation
            ? TimeSpan.FromMinutes(1)
            : GetPollDelayBasedOnGameStatus(updatedGame);

        var logMessage = new StringBuilder($"Processing {updatedGame.AwayTeam.Abbreviation} @ {updatedGame.HomeTeam.Abbreviation} complete. ");
        var pollingMessage = pollAgainDelay == null ? "Stopping polling." : $"Polling again in {pollAgainDelay.Value.TotalSeconds} seconds.";
        logMessage.Append(pollingMessage);
        logger.LogInformation(logMessage.ToString());

        await TriggerPregameAlerts(updatedGame, _appConfig.CurrentTimeUtc, ct);

        if (pollAgainDelay != null)
        {
            var nextRunTime = DateTime.UtcNow.Add(pollAgainDelay.Value);
            await gameCache.SetNextRunAsync(updatedGame.Id, nextRunTime);
            await nhlQueue.SendMessageAsync(new ProcessGameMessage(request.GameId, false), pollAgainDelay, ct);
        }
        else
        {
            await gameCache.RemoveNextRunAsync(updatedGame.Id);
        }
    }

    private async Task<List<PlayerDto>> FetchPlayerDetails(List<PlayerSummaryDto> summaries)
    {
        if (summaries.Count == 0)
            return [];

        var fetchedPlayers = new List<PlayerDto>();

        foreach (var summary in summaries)
        {
            var player = await nhlClient.GetPlayerAsync(summary.Id);

            if (player != null)
                fetchedPlayers.Add(player);
        }

        return fetchedPlayers;
    }

    private static Dictionary<int, GoalSummaryDto> GetGoalSummaries(GameDto game)
    {
        if (game.HomeTeam.Roster.Count == 0 && game.AwayTeam.Roster.Count == 0)
            return [];

        return game.Plays
            .Where(play =>
                play.Type == PlayType.Goal
                && play.PrimaryPlayerId != null
                && play.PeriodType is PeriodType.Regulation or PeriodType.Overtime)
            .DistinctBy(play => play.Id)
            .Select(play => new { play.Id, Player = GetPlayerById(game, play.PrimaryPlayerId!.Value), play.TimeInPeriod })
            .Where(x => x.Player != null)
            .ToDictionary(
                k => k.Id,
                v => new GoalSummaryDto { Player = v.Player!, PeriodTime = v.TimeInPeriod }
            );
    }

    private static PlayerDto? GetPlayerById(GameDto game, int id)
    {
        var allPlayers = game.HomeTeam.Roster.Concat(game.AwayTeam.Roster);
        return allPlayers.FirstOrDefault(p => p.Id == id);
    }

    private static bool IsSameGoal(GoalSummaryDto goalSummary1, GoalSummaryDto goalSummary2)
    {
        var isSamePlayer = goalSummary1.Player.Id == goalSummary2.Player.Id;
        var isSameTime = Math.Abs(GetPeriodTimeInSeconds(goalSummary1.PeriodTime) - GetPeriodTimeInSeconds(goalSummary2.PeriodTime)) <= 3;
        return isSamePlayer && isSameTime;
    }

    private static int GetPeriodTimeInSeconds(string periodTime)
    {
        if (string.IsNullOrEmpty(periodTime) || !periodTime.Contains(':')) return 0;
        var parts = periodTime.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var minutes) || !int.TryParse(parts[1], out var seconds)) return 0;
        return minutes * 60 + seconds;
    }

    private async Task HandleScoringUpdatesAsync(Dictionary<int, GoalSummaryDto> before, Dictionary<int, GoalSummaryDto> after, GameDto game, CancellationToken ct)
    {
        //check for new and changed goals
        foreach (var (id, goalAfter) in after)
        {
            var playDto = game.Plays.First(p => p.Id == id);
            var scorerDto = goalAfter.Player;

            if (!before.TryGetValue(id, out var goalBefore))
            {
                await mediator.Publish(new GoalScoredNotification(new(game.Id, playDto, scorerDto)), ct);
            }
            else if (!IsSameGoal(goalBefore, goalAfter))
            {
                var oldScorerDto = goalBefore.Player;
                await mediator.Publish(new GoalChangedNotification(new(game.Id, playDto, scorerDto, oldScorerDto)), ct);
                await mediator.Publish(new GoalScoredNotification(new(game.Id, playDto, scorerDto)), ct);
            }
        }

        //check for removed goals
        foreach (var (id, goalBefore) in before)
        {
            if (!after.TryGetValue(id, out var goalAfter) || !IsSameGoal(goalBefore, goalAfter))
            {
                var scorerDto = goalBefore.Player;
                await mediator.Publish(new GoalRemovedNotification(new(game.Id, id, scorerDto)), ct);
            }
        }
    }

    private async Task TriggerPregameAlerts(GameDto game, DateTime utcNow, CancellationToken ct)
    {
        var homeComplete = game.HomeTeam.Roster.Count == game.PlayerSummaries.Count(ps => ps.TeamId == game.HomeTeam.Id);
        var awayComplete = game.AwayTeam.Roster.Count == game.PlayerSummaries.Count(ps => ps.TeamId == game.AwayTeam.Id);
        var isWithin30Min = (game.DateTime - utcNow) <= TimeSpan.FromMinutes(30)
                            && (game.DateTime - utcNow) > TimeSpan.Zero;

        if (homeComplete && awayComplete && isWithin30Min)
        {
            var alreadyTriggered = await gameCache.HasPreGameAlertTriggeredAsync(game.Id);
            if (!alreadyTriggered)
            {
                await mediator.Publish(new PicksReadyNotification(game), ct);
                await gameCache.SetPreGameAlertTriggeredAsync(game.Id);

                logger.LogInformation(
                    "Picks ready for {away} @ {home}. Triggered pregame alert event.",
                    game.AwayTeam.Abbreviation,
                    game.HomeTeam.Abbreviation
                );
            }
        }
    }

    private TimeSpan? GetPollDelayBasedOnGameStatus(GameDto game)
    {
        if (game.GameState == GameState.Final) return null;
        if (game.GameState == GameState.Live) return TimeSpan.FromSeconds(10);
        if (game.GameState == GameState.Upcoming)
        {
            if (game.DateTime - _appConfig.CurrentTimeUtc <= TimeSpan.FromMinutes(40))
                return TimeSpan.FromMinutes(1);
            else
                return TimeSpan.FromMinutes(10);
        }

        return null;
    }

    private async Task HandleRemovedPlayersAsync(GameDto cachedGame, GameDto updatedGame, IReadOnlyList<PlayerSummaryDto> updatedHomeSummaries, IReadOnlyList<PlayerSummaryDto> updatedAwaySummaries, CancellationToken ct)
    {
        var oldHome = cachedGame.HomeTeam.Roster;
        var oldAway = cachedGame.AwayTeam.Roster;

        var newHomeIds = updatedHomeSummaries.Select(s => s.Id).ToHashSet();
        var newAwayIds = updatedAwaySummaries.Select(s => s.Id).ToHashSet();

        var removedHomePlayers = oldHome.Where(p => !newHomeIds.Contains(p.Id)).ToList();
        var removedAwayPlayers = oldAway.Where(p => !newAwayIds.Contains(p.Id)).ToList();
        var removedPlayers = removedHomePlayers.Concat(removedAwayPlayers).ToList();

        if (removedPlayers.Count == 0)
            return;

        var removedPlayerIds = removedPlayers.Select(p => p.Id).ToList();

        var picksToRemove = await dbContext.LobbyMemberPicks
            .AsNoTracking()
            .Include(p => p.LobbyMember)
                .ThenInclude(lm => lm.Lobby)
            .Where(p => removedPlayerIds.Contains(p.PlayerId)
                        && p.GameId == updatedGame.Id
                        && p.IsActive)
            .ToListAsync(ct);

        foreach (var pick in picksToRemove)
        {
            await mediator.Send(new RemovePickCommand
            {
                PickId = pick.Id,
                Code = pick.LobbyMember.Lobby.JoinCode,
                UserId = pick.LobbyMember.UserId
            }, ct);
        }

        logger.LogInformation(
            "Removed {count} picks because players left the lineup for {away} @ {home}.",
            picksToRemove.Count,
            updatedGame.AwayTeam.Abbreviation,
            updatedGame.HomeTeam.Abbreviation
        );
    }
}
