using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;
using StackExchange.Redis;
using System.Text.Json;

namespace DraftPuck.Infrastructure.Redis;

public class RedisGameCache(IDatabase redisDb) : IGameCache
{
    private const string GamePrefix = "game:";
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

    public async Task<List<GameDto>> GetAllGamesAsync()
    {
        var endpoint = redisDb.Multiplexer.GetEndPoints().SingleOrDefault();

        if (endpoint == null)
            return [];

        var server = redisDb.Multiplexer.GetServer(endpoint);
        var asyncKeys = server.KeysAsync(database: redisDb.Database, pattern: $"{GamePrefix}*");
        var keysList = new List<RedisKey>();

        await foreach (var key in asyncKeys)
        {
            keysList.Add(key);
        }

        var keys = keysList.ToArray();

        if (keys.Length == 0)
            return [];

        try
        {
            var values = await redisDb.StringGetAsync(keys);
            var games = new List<GameDto>(values.Length);

            foreach (var value in values)
            {
                if (value.IsNullOrEmpty) continue;

                var game = JsonSerializer.Deserialize<GameDto>(value!);

                if (game != null)
                    games.Add(game);
            }

            return games;
        }
        catch (Exception ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }

    public async Task RemoveGameAsync(GameDto game)
        => await RemoveGameAsync(game.Id);

    public async Task RemoveGameAsync(int id)
    {
        var key = $"{GamePrefix}{id}";
        var nextRunKey = $"{NextRunPrefix}{id}";
        var pregameTriggerKey = $"{PregameAlertTriggeredPrefix}{id}";

        await redisDb.KeyDeleteAsync([key, nextRunKey, pregameTriggerKey]);
    }

    public async Task AddGameAsync(GameDto game) => await UpdateGameAsync(game);

    public async Task UpdateGameAsync(GameDto game)
    {
        var key = $"{GamePrefix}{game.Id}";
        var json = JsonSerializer.Serialize(game);
        await redisDb.StringSetAsync(key, json, expiry: TimeSpan.FromDays(1));
    }

    public async Task<DateTime?> GetNextRunAsync(int gameId)
    {
        var key = $"{NextRunPrefix}{gameId}";
        var value = await redisDb.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        if (DateTime.TryParse(value!, out var nextRun))
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

    public async Task<bool> HasPreGameAlertTriggeredAsync(int gameId)
    {
        var key = $"{PregameAlertTriggeredPrefix}{gameId}";
        return await redisDb.KeyExistsAsync(key);
    }

    public async Task SetPreGameAlertTriggeredAsync(int gameId)
    {
        var key = $"{PregameAlertTriggeredPrefix}{gameId}";
        await redisDb.StringSetAsync(key, "1", expiry: TimeSpan.FromDays(1));
    }

    public async Task<bool> HasUserBeenNotifiedRecentlyAsync(Guid userId)
    {
        var key = $"{UserPicksThrottlePrefix}{userId}";
        return await redisDb.KeyExistsAsync(key);
    }

    public async Task MarkUserAsNotifiedAsync(Guid userId)
    {
        var key = $"{UserPicksThrottlePrefix}{userId}";
        await redisDb.StringSetAsync(key, "1", expiry: TimeSpan.FromMinutes(15));
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
}