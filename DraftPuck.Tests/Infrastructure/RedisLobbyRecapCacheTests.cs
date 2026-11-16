using DraftPuck.Application.Features.Lobbies.Recap;
using DraftPuck.Infrastructure.Redis;

namespace DraftPuck.Tests.Infrastructure;

[Collection(RedisCollection.Name)]
public class RedisLobbyRecapCacheTests(RedisFixture fixture)
{
    private RedisLobbyRecapCache Cache() => new(fixture.Connection.GetDatabase());

    [Fact]
    public async Task StoresRecapsUntilTheyExpire()
    {
        var lobbyId = Guid.NewGuid();
        var recap = new LobbyRecapDto
        {
            Awards = [new LobbyAwardDto { Key = LobbyAwards.BestPick, Title = "Best pick", MemberId = Guid.NewGuid(), MemberName = "Sam", Detail = "McDavid (EDM) earned 4 drinks" }]
        };

        Assert.Null(await Cache().GetAsync(lobbyId));
        await Cache().SetAsync(lobbyId, recap, TimeSpan.FromMinutes(10));

        var cached = await Cache().GetAsync(lobbyId);
        Assert.Equal(("Sam", "McDavid (EDM) earned 4 drinks"), (Assert.Single(cached!.Awards).MemberName, cached.Awards[0].Detail));
        var ttl = await fixture.Connection.GetDatabase().KeyTimeToLiveAsync($"lobby:recap:v{LobbyAwards.Version}:{lobbyId}");
        Assert.InRange(ttl!.Value, TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(10));
    }
}
