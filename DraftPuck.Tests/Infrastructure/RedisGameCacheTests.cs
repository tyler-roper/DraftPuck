using DraftPuck.Infrastructure.Redis;

namespace DraftPuck.Tests.Infrastructure;

[Collection(RedisCollection.Name)]
public class RedisGameCacheTests(RedisFixture fixture)
{
    private RedisGameCache Cache() => new(fixture.Connection.GetDatabase());

    [Fact]
    public async Task StoresAndReturnsGames()
    {
        var game = TestGames.Create(Seed.NewGameId(), new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc), GameState.Live);
        game.Plays = [TestGames.Goal(1, 1, "05:00", TestGames.HomePlayer(0), TestGames.HomeTeamId)];

        await Cache().AddGameAsync(game);
        var cached = await Cache().GetGameByIdAsync(game.Id);

        Assert.NotNull(cached);
        Assert.Equal(game.DateTime, cached.DateTime);
        Assert.Equal(GameState.Live, cached.GameState);
        Assert.Equal(game.HomeTeam.Roster.Select(p => p.Id), cached.HomeTeam.Roster.Select(p => p.Id));
        Assert.Equal(TestGames.HomePlayer(0), Assert.Single(cached.Plays).PrimaryPlayerId);
        Assert.Contains(await Cache().GetAllGamesAsync(), g => g.Id == game.Id);
    }

    [Fact]
    public async Task ReturnsNullForUnknownGames()
    {
        Assert.Null(await Cache().GetGameByIdAsync(Seed.NewGameId()));
    }

    [Fact]
    public async Task NextRunRoundTripsAsUtc()
    {
        var gameId = Seed.NewGameId();
        var nextRun = new DateTime(2026, 11, 13, 1, 2, 3, DateTimeKind.Utc);

        await Cache().SetNextRunAsync(gameId, nextRun);
        var stored = await Cache().GetNextRunAsync(gameId);

        // ProcessGameCommandHandler compares this with DateTime.UtcNow, so it must come back as the same UTC instant.
        Assert.Equal(nextRun, stored);
        Assert.Equal(DateTimeKind.Utc, stored!.Value.Kind);
    }

    [Fact]
    public async Task RemovingAGameClearsItsScheduleAndAlertFlag()
    {
        var game = TestGames.Create(Seed.NewGameId(), DateTime.UtcNow);
        var cache = Cache();
        await cache.AddGameAsync(game);
        await cache.SetNextRunAsync(game.Id, DateTime.UtcNow);
        await cache.TryClaimPreGameAlertAsync(game.Id);

        await cache.RemoveGameAsync(game.Id);

        Assert.Null(await cache.GetGameByIdAsync(game.Id));
        Assert.Null(await cache.GetNextRunAsync(game.Id));
        Assert.True(await cache.TryClaimPreGameAlertAsync(game.Id));
    }

    [Fact]
    public async Task PregameAlertCanBeClaimedOnceUntilReleased()
    {
        var gameId = Seed.NewGameId();
        var cache = Cache();

        Assert.True(await cache.TryClaimPreGameAlertAsync(gameId));
        Assert.False(await cache.TryClaimPreGameAlertAsync(gameId));

        await cache.ReleasePreGameAlertAsync(gameId);
        Assert.True(await cache.TryClaimPreGameAlertAsync(gameId));
    }

    [Fact]
    public async Task ConcurrentClaimsHaveExactlyOneWinner()
    {
        // Games starting together are processed in parallel; only one may send the user's picks-ready push.
        var userId = Guid.NewGuid();
        var gameId = Seed.NewGameId();

        var userClaims = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => Cache().TryClaimUserPicksNotificationAsync(userId)));
        var gameClaims = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => Cache().TryClaimPreGameAlertAsync(gameId)));

        Assert.Single(userClaims, claimed => claimed);
        Assert.Single(gameClaims, claimed => claimed);
    }

    [Fact]
    public async Task UserClaimCanBeReleasedForARetry()
    {
        var userId = Guid.NewGuid();
        var cache = Cache();

        Assert.True(await cache.TryClaimUserPicksNotificationAsync(userId));
        await cache.ReleaseUserPicksNotificationAsync(userId);

        Assert.True(await cache.TryClaimUserPicksNotificationAsync(userId));
    }

    [Fact]
    public async Task TracksWhichGamesAUserWasNotifiedFor()
    {
        var userId = Guid.NewGuid();
        var gameId = Seed.NewGameId();
        var cache = Cache();

        Assert.False(await cache.HasUserBeenNotifiedForGameAsync(userId, gameId));
        await cache.MarkUserNotifiedForGameAsync(userId, gameId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30));

        Assert.True(await cache.HasUserBeenNotifiedForGameAsync(userId, gameId));
        Assert.False(await cache.HasUserBeenNotifiedForGameAsync(userId, Seed.NewGameId()));

        // Kept until an hour after the start.
        var ttl = await fixture.Connection.GetDatabase().KeyTimeToLiveAsync($"userGamePicksNotified:{userId}:{gameId}");
        Assert.InRange(ttl!.Value, TimeSpan.FromMinutes(89), TimeSpan.FromMinutes(90));
    }
}
