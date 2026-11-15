using DraftPuck.Application.Features.Games;
using DraftPuck.Infrastructure.Redis;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Games;

/// <summary>
/// Each game is polled by a chain of queue messages, every poll queuing the next. If one is lost (e.g. moved to the
/// poison queue), nothing would poll that game again until the next morning's schedule load; this restarts the chain.
/// Runs against real Redis, which other tests share, so each test checks only its own games.
/// </summary>
[Collection(RedisCollection.Name)]
public class RestartStalledPollingTests(RedisFixture fixture)
{
    private readonly INhlQueueService _queue = Substitute.For<INhlQueueService>();
    private RedisGameCache Cache() => new(fixture.Connection.GetDatabase());

    private Task RunAsync() => new RestartStalledPollingCommandHandler(
        Cache(), _queue, Options.Create(new ApplicationOptions()), NullLogger<RestartStalledPollingCommandHandler>.Instance)
        .Handle(new RestartStalledPollingCommand(), default);

    // Starting "now" keeps the game on the current NHL day (see GameProcessingHelpers.IsGameCurrent).
    private async Task<int> CachedGameAsync(GameState state, DateTime? nextRun, DateTime? start = null)
    {
        var game = TestGames.Create(Seed.NewGameId(), start ?? DateTime.UtcNow, state);
        await Cache().AddGameAsync(game);
        if (nextRun != null)
            await Cache().SetNextRunAsync(game.Id, nextRun.Value);
        return game.Id;
    }

    private int TimesRestarted(int gameId) => _queue.ReceivedCalls()
        .Count(c => c.GetArguments()[0] is ProcessGameMessage { IsInitialPopulation: false } m && m.GameId == gameId);

    [Fact]
    public async Task RestartsPollingOfALiveGameWhosePollIsWellOverdue()
    {
        var gameId = await CachedGameAsync(GameState.Live, nextRun: DateTime.UtcNow.AddMinutes(-6));

        await RunAsync();

        Assert.Equal(1, TimesRestarted(gameId));
    }

    [Fact]
    public async Task RestartsPollingOfAnUnfinishedGameWithNoPollScheduled()
    {
        var gameId = await CachedGameAsync(GameState.Upcoming, nextRun: null);

        await RunAsync();

        Assert.Equal(1, TimesRestarted(gameId));
    }

    // A poll can run for minutes (e.g. fetching every player on a cold cache), so a little late isn't stalled.
    [Fact]
    public async Task LeavesGamesThatAreBeingPolled()
    {
        var scheduled = await CachedGameAsync(GameState.Live, nextRun: DateTime.UtcNow.AddSeconds(10));
        var runningLong = await CachedGameAsync(GameState.Live, nextRun: DateTime.UtcNow.AddMinutes(-2));

        await RunAsync();

        Assert.Equal(0, TimesRestarted(scheduled));
        Assert.Equal(0, TimesRestarted(runningLong));
    }

    [Fact]
    public async Task LeavesFinishedGamesAndOtherDaysGames()
    {
        var final = await CachedGameAsync(GameState.Final, nextRun: null);
        var lastWeek = await CachedGameAsync(GameState.Live, nextRun: DateTime.UtcNow.AddDays(-7), start: DateTime.UtcNow.AddDays(-7));

        await RunAsync();

        Assert.Equal(0, TimesRestarted(final));
        Assert.Equal(0, TimesRestarted(lastWeek));
    }

    // It runs every minute, and the restarted poll may not have run yet by the next check.
    [Fact]
    public async Task RestartsAStalledGameOnlyOnce()
    {
        var gameId = await CachedGameAsync(GameState.Live, nextRun: DateTime.UtcNow.AddMinutes(-6));

        await RunAsync();
        await RunAsync();

        Assert.Equal(1, TimesRestarted(gameId));
    }
}
