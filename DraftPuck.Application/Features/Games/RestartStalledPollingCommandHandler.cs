using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Games;

/// <summary>
/// Each game is polled by a chain of queue messages: every poll records when the next is due and queues it. If one is
/// lost (moved to the poison queue after repeated failures, or the queue lost it), nothing polls that game again until
/// the next morning's schedule load. This finds today's unfinished games whose next poll is well overdue, or that have
/// none scheduled, and queues a poll to restart the chain.
/// </summary>
public class RestartStalledPollingCommandHandler(
    IGameCache gameCache,
    INhlQueueService nhlQueue,
    IOptions<ApplicationOptions> appConfig,
    ILogger<RestartStalledPollingCommandHandler> logger) : IRequestHandler<RestartStalledPollingCommand>
{
    /// <summary>
    /// How late a poll must be before its chain counts as stopped. A poll can run for minutes (fetching every player on a
    /// cold cache), and restarting a chain that's still running would poll the game twice as often for a while.
    /// </summary>
    public static readonly TimeSpan StalledAfter = TimeSpan.FromMinutes(5);

    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(RestartStalledPollingCommand request, CancellationToken ct)
    {
        // Real time: next-run times are, unlike test mode's simulated clock.
        var now = DateTime.UtcNow;

        foreach (var game in await gameCache.GetAllGamesAsync())
        {
            if (game.GameState == GameState.Final || !GameProcessingHelpers.IsGameCurrent(game, _appConfig.CurrentTimeUtc))
                continue;

            var nextRun = await gameCache.GetNextRunAsync(game.Id);
            if (nextRun != null && now - nextRun.Value < StalledAfter)
                continue;

            logger.LogWarning(
                "Polling of {away} @ {home} ({gameId}) had stopped (next poll due {nextRun}). Restarting it.",
                game.AwayTeam.Abbreviation, game.HomeTeam.Abbreviation, game.Id, nextRun?.ToString("u") ?? "never");

            // Due now, so the restarted poll runs (a poll skips itself when another is scheduled later), and so the next
            // check doesn't restart it again before it has run.
            await gameCache.SetNextRunAsync(game.Id, now);
            await nhlQueue.SendMessageAsync(new ProcessGameMessage(game.Id, false), ct: ct);
        }
    }
}
