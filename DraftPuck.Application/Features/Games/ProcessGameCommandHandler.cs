using DraftPuck.Application.Features.Lobbies.Picks;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Games;

/// <summary>
/// One poll of a game: fetch it from the NHL, sync rosters, announce goal changes, send pregame alerts, and queue the
/// next poll. The previous poll's copy of the game lives in the game cache.
/// </summary>
public class ProcessGameCommandHandler(
    IGameCache gameCache,
    IMediator mediator,
    INhlClient nhlClient,
    INhlQueueService nhlQueue,
    GameRosterSync rosterSync,
    IOptions<ApplicationOptions> appConfig,
    ILogger<ProcessGameCommandHandler> logger) : IRequestHandler<ProcessGameCommand>
{
    private static readonly TimeSpan FallbackDelay = TimeSpan.FromMinutes(2);
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(ProcessGameCommand request, CancellationToken ct)
    {
        try
        {
            await PollAsync(request, ct);
        }
        catch (Exception ex) when (DescribeNhlApiFailure(ex, ct) is { } reason)
        {
            // The NHL API being down or slow is outside our control and the retry handles it,
            // so it's a warning without a stack trace rather than an error.
            logger.LogWarning(
                "NHL API unavailable for game {gameId} ({reason}). Scheduling fallback retry in {delay}.",
                request.GameId, reason, FallbackDelay);

            await QueuePollAsync(request.GameId, FallbackDelay, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing game {gameId}. Scheduling fallback retry.",
                request.GameId);

            await QueuePollAsync(request.GameId, FallbackDelay, ct);
        }
    }

    /// <summary>
    /// Returns a short reason when the exception is the NHL API failing (an error status code or
    /// an HttpClient timeout), or null for anything else, which is a real bug.
    /// </summary>
    private static string? DescribeNhlApiFailure(Exception ex, CancellationToken ct) => ex switch
    {
        HttpRequestException { StatusCode: { } status } => $"{(int)status} {status}",
        HttpRequestException http => http.Message,
        TaskCanceledException { InnerException: TimeoutException } when !ct.IsCancellationRequested => "request timed out",
        _ => null
    };

    private async Task PollAsync(ProcessGameCommand request, CancellationToken ct)
    {
        var game = await nhlClient.GetFullGameAsync(request.GameId);
        if (game == null) return;

        var matchup = $"{game.AwayTeam.Abbreviation} @ {game.HomeTeam.Abbreviation}";

        var nextRun = await gameCache.GetNextRunAsync(request.GameId);
        if (nextRun > DateTime.UtcNow)
        {
            logger.LogInformation("Skipping {matchup} because another process is already scheduled at {nextRun}.", matchup, nextRun.Value);
            return;
        }

        if (!GameProcessingHelpers.IsGameCurrent(game, _appConfig.CurrentTimeUtc))
        {
            await gameCache.RemoveGameAsync(game.Id);
            logger.LogInformation("{matchup} has been removed from the cache.", matchup);
            return;
        }

        logger.LogInformation("Processing {matchup}...", matchup);

        // Null on the first poll, or when the cached copy was lost mid-game.
        var previous = await gameCache.GetGameByIdAsync(request.GameId);

        // On the initial schedule load, skip fetching every player so the queue isn't blocked until each game is cached.
        await rosterSync.SyncAsync(previous ?? game, game, fetchChangedRosters: !request.IsInitialPopulation, ct);

        GameProcessingHelpers.SetPlayDateTimes(game, _appConfig.CurrentTimeUtc, _appConfig.IsTestMode, previous ?? game);
        GameProcessingHelpers.HandleTestMode(game, _appConfig);

        // With no previous poll every goal is treated as new. Awarding skips goals that already have drinks,
        // so this only fills in goals missed while there was no cached copy.
        var goalChanges = GoalMatcher.Compare(previous == null ? [] : TrackedGoal.CountedIn(previous), TrackedGoal.CountedIn(game));

        await gameCache.UpdateGameAsync(game);
        await PublishGoalChangesAsync(game.Id, goalChanges, ct);

        var pollAgainDelay = GameProcessingHelpers.GetPollDelay(game, _appConfig.CurrentTimeUtc, request.IsInitialPopulation);
        logger.LogInformation("Processing {matchup} complete. {polling}", matchup,
            pollAgainDelay == null ? "Stopping polling." : $"Polling again in {pollAgainDelay.Value.TotalSeconds} seconds.");

        await TriggerPregameEventsAsync(game, _appConfig.CurrentTimeUtc, ct);

        if (pollAgainDelay != null)
        {
            await gameCache.SetNextRunAsync(game.Id, DateTime.UtcNow.Add(pollAgainDelay.Value));
            await QueuePollAsync(game.Id, pollAgainDelay.Value, ct);
        }
        else
        {
            await gameCache.RemoveNextRunAsync(game.Id);
        }
    }

    private Task QueuePollAsync(int gameId, TimeSpan delay, CancellationToken ct)
        => nhlQueue.SendMessageAsync(new ProcessGameMessage(gameId, false), delay, ct);

    /// <summary>
    /// A scorer change revokes the old scorer's drinks (GoalChanged) before awarding the new scorer's (GoalScored).
    /// Renumbers go first so revokes and awards find drinks under the goal's current play ID.
    /// </summary>
    private async Task PublishGoalChangesAsync(int gameId, List<GoalChange> changes, CancellationToken ct)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case GoalRenumbered r:
                    await mediator.Publish(new GoalRenumberedNotification(gameId, r.OldPlayId, r.NewPlayId), ct);
                    break;
                case GoalScorerChanged c:
                    await mediator.Publish(new GoalChangedNotification(new(gameId, c.After.Play, c.After.Scorer, c.Before.Scorer)), ct);
                    await mediator.Publish(new GoalScoredNotification(new(gameId, c.After.Play, c.After.Scorer)), ct);
                    break;
                case GoalAdded a:
                    await mediator.Publish(new GoalScoredNotification(new(gameId, a.Goal.Play, a.Goal.Scorer)), ct);
                    break;
                case GoalCalledBack b:
                    await mediator.Publish(new GoalRemovedNotification(new(gameId, b.Goal.PlayId, b.Goal.Scorer)), ct);
                    break;
            }
        }
    }

    private async Task TriggerPregameEventsAsync(GameDto game, DateTime utcNow, CancellationToken ct)
    {
        var rostersComplete =
            game.HomeTeam.Roster.Count == game.PlayerSummaries.Count(ps => ps.TeamId == game.HomeTeam.Id)
            && game.AwayTeam.Roster.Count == game.PlayerSummaries.Count(ps => ps.TeamId == game.AwayTeam.Id);
        if (!rostersComplete) return;

        // Bot picks run on every poll, so bots added mid-game, or games whose rosters complete after puck drop, still
        // get picks. Bots only fill their remaining picks, so repeat runs do nothing. A failure here must not block the
        // picks-ready alert below.
        try
        {
            await mediator.Publish(new BotPicksReadyNotification(game), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Bot picks failed for {away} @ {home}.", game.AwayTeam.Abbreviation, game.HomeTeam.Abbreviation);
        }

        var isBeforePuckDropInPickWindow = GameProcessingHelpers.IsPickWindowOpen(game, utcNow) && game.DateTime > utcNow;
        if (!isBeforePuckDropInPickWindow) return;

        // Claim atomically so overlapping runs for the same game can't both publish. Release on failure so a retry can.
        if (!await gameCache.TryClaimPreGameAlertAsync(game.Id)) return;

        try
        {
            await mediator.Publish(new PicksReadyNotification(game), ct);
        }
        catch
        {
            await gameCache.ReleasePreGameAlertAsync(game.Id);
            throw;
        }

        logger.LogInformation("Picks ready for {away} @ {home}. Triggered pregame alert event.", game.AwayTeam.Abbreviation, game.HomeTeam.Abbreviation);
    }
}
