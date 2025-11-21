using DraftPuck.Application.Features.Discord;
using StackExchange.Redis;

namespace DraftPuck.Infrastructure.Redis;

public class RedisOAuthCache(IDatabase redisDb) : IOAuthCache
{
    private const string DiscordStatePrefix = "oauth:discord:state:";

    public async Task AddStateAsync(Guid state, Guid userId)
    {
        var key = $"{DiscordStatePrefix}{state}";
        await redisDb.StringSetAsync(key, userId.ToString(), expiry: TimeSpan.FromMinutes(5));
    }

    public async Task<Guid?> GetUserIdAndDeleteByState(string state)
    {
        var key = $"{DiscordStatePrefix}{state}";
        var value = await redisDb.StringGetDeleteAsync(key);
        if (value.IsNullOrEmpty || !Guid.TryParse(value, out var result))
            return null;

        return result;
    }

    public async Task RemoveStateAsync(string state)
    {
        var key = $"{DiscordStatePrefix}{state}";
        await redisDb.KeyDeleteAsync(key);
    }
}