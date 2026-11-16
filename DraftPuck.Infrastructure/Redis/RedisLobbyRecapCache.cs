using System.Text.Json;
using DraftPuck.Application.Features.Lobbies.Recap;
using StackExchange.Redis;

namespace DraftPuck.Infrastructure.Redis;

public class RedisLobbyRecapCache(IDatabase redisDb) : ILobbyRecapCache
{
    // The awards' version is part of the key, so changing the rules recomputes every cached recap.
    private static string Key(Guid lobbyId) => $"lobby:recap:v{LobbyAwards.Version}:{lobbyId}";

    public async Task<LobbyRecapDto?> GetAsync(Guid lobbyId)
    {
        var json = await redisDb.StringGetAsync(Key(lobbyId));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<LobbyRecapDto>((string)json!);
    }

    public async Task SetAsync(Guid lobbyId, LobbyRecapDto recap, TimeSpan expiry) =>
        await redisDb.StringSetAsync(Key(lobbyId), JsonSerializer.Serialize(recap), expiry);
}
