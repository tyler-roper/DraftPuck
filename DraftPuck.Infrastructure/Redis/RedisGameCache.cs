using System.Globalization;
using DraftPuck.Application.Features.Games;
using StackExchange.Redis;
using System.Text.Json;

namespace DraftPuck.Infrastructure.Redis;

public class RedisGameCache(IDatabase redisDb) : IGameCache
{
    private const string GamePrefix = "game:";
    /// <summary>A set of the IDs of cached games, so they can be listed without scanning keys.</summary>
    private const string GameIndexKey = "games";
    private const string NextRunPrefix = "nextRun:";
    private const string PregameAlertTriggeredPrefix = "pregameAlertTriggered:";
    private const string UserPicksThrottlePrefix = "userPicksThrottle:";
    private const string UserGamePicksNotifiedPrefix = "userGamePicksNotified:";

    public async Task<GameDto?> GetGameByIdAsync(int id)
    {
        var key = $"{GamePrefix}{id}";
        var json = await redisDb.StringGetAsync(key);

        if (json.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<GameDto>(json!) ?? null;
    }

    /// <summary>
    /// Every cached game, found through the <see cref="GameIndexKey"/> set rather than by scanning keys, which only
    /// works on a single-node Redis and slows as the keyspace grows. IDs whose game has expired are dropped as found.
    /// </summary>
    public async Task<List<GameDto>> GetAllGamesAsync()
    {
        var ids = (await redisDb.SetMembersAsync(GameIndexKey)).Select(id => (int)id).ToArray();
        if (ids.Length == 0)
            return [];

        var values = await redisDb.StringGetAsync([.. ids.Select(id => (RedisKey)GameKey(id))]);
        var games = new List<GameDto>(values.Length);
        var expiredIds = new List<RedisValue>();

        for (var i = 0; i < ids.Length; i++)
        {
            if (values[i].IsNullOrEmpty)
            {
                expiredIds.Add(ids[i]);
                continue;
            }

            if (JsonSerializer.Deserialize<GameDto>(values[i]!) is { } game)
                games.Add(game);
        }

        if (expiredIds.Count > 0)
            await redisDb.SetRemoveAsync(GameIndexKey, [.. expiredIds]);

        return games;
    }

    public async Task RemoveGameAsync(GameDto game)
        => await RemoveGameAsync(game.Id);

    public async Task RemoveGameAsync(int id)
    {
        var key = $"{GamePrefix}{id}";
        var nextRunKey = $"{NextRunPrefix}{id}";
        var pregameTriggerKey = $"{PregameAlertTriggeredPrefix}{id}";

        await redisDb.KeyDeleteAsync([key, nextRunKey, pregameTriggerKey]);
        await redisDb.SetRemoveAsync(GameIndexKey, id);
    }

    public async Task AddGameAsync(GameDto game) => await UpdateGameAsync(game);

    public async Task UpdateGameAsync(GameDto game)
    {
        var json = JsonSerializer.Serialize(game);
        await redisDb.StringSetAsync(GameKey(game.Id), json, expiry: TimeSpan.FromDays(1));
        await redisDb.SetAddAsync(GameIndexKey, game.Id);
    }

    public async Task<DateTime?> GetNextRunAsync(int gameId)
    {
        var key = $"{NextRunPrefix}{gameId}";
        var value = await redisDb.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        // Stored with "O" (UTC); RoundtripKind keeps it UTC instead of converting to the machine's local time.
        if (DateTime.TryParse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var nextRun))
            return nextRun;

        return null;
    }

    public async Task SetNextRunAsync(int gameId, DateTime nextRun)
    {
        var key = $"{NextRunPrefix}{gameId}";
        await redisDb.StringSetAsync(key, nextRun.ToString("O"), expiry: TimeSpan.FromDays(1));
    }

    public async Task RemoveNextRunAsync(int gameId)
    {
        var key = $"{NextRunPrefix}{gameId}";
        await redisDb.KeyDeleteAsync(key);
    }

    public async Task<bool> TryClaimPreGameAlertAsync(int gameId)
    {
        var key = $"{PregameAlertTriggeredPrefix}{gameId}";
        return await redisDb.StringSetAsync(key, "1", expiry: TimeSpan.FromDays(1), when: When.NotExists);
    }

    public async Task ReleasePreGameAlertAsync(int gameId)
    {
        var key = $"{PregameAlertTriggeredPrefix}{gameId}";
        await redisDb.KeyDeleteAsync(key);
    }

    public async Task<bool> TryClaimUserPicksNotificationAsync(Guid userId)
    {
        var key = $"{UserPicksThrottlePrefix}{userId}";
        return await redisDb.StringSetAsync(key, "1", expiry: TimeSpan.FromMinutes(15), when: When.NotExists);
    }

    public async Task ReleaseUserPicksNotificationAsync(Guid userId)
    {
        var key = $"{UserPicksThrottlePrefix}{userId}";
        await redisDb.KeyDeleteAsync(key);
    }

    public async Task<bool> HasUserBeenNotifiedForGameAsync(Guid userId, int gameId)
    {
        var key = $"{UserGamePicksNotifiedPrefix}{userId}:{gameId}";
        return await redisDb.KeyExistsAsync(key);
    }

    public async Task MarkUserNotifiedForGameAsync(Guid userId, int gameId, DateTime utcNow, DateTime gameStartUtc)
    {
        var key = $"{UserGamePicksNotifiedPrefix}{userId}:{gameId}";
        var expiry = gameStartUtc - utcNow + TimeSpan.FromHours(1);
        if (expiry < TimeSpan.Zero)
            expiry = TimeSpan.FromHours(1);
        await redisDb.StringSetAsync(key, "1", expiry);
    }

    private static string GameKey(int id) => $"{GamePrefix}{id}";
}
