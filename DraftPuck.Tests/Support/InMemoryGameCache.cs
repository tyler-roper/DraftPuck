using System.Text.Json;
using DraftPuck.Application.Features.Games;

namespace DraftPuck.Tests.Support;

/// <summary>
/// An <see cref="IGameCache"/> that behaves like the Redis one for a single test: games round-trip through JSON,
/// so the handler can't share object references between polls, and claims are set-if-not-exists.
/// Next-run times are recorded but never block a poll, so tests can poll back to back.
/// </summary>
public class InMemoryGameCache : IGameCache
{
    private readonly Dictionary<int, string> _games = [];
    private readonly HashSet<string> _claims = [];

    public Task<GameDto?> GetGameByIdAsync(int gameId)
        => Task.FromResult(_games.TryGetValue(gameId, out var json) ? JsonSerializer.Deserialize<GameDto>(json) : null);

    public Task<List<GameDto>> GetAllGamesAsync()
        => Task.FromResult(_games.Values.Select(json => JsonSerializer.Deserialize<GameDto>(json)!).ToList());

    public Task RemoveGameAsync(int gameId)
    {
        _games.Remove(gameId);
        return Task.CompletedTask;
    }

    public Task RemoveGameAsync(GameDto game) => RemoveGameAsync(game.Id);

    public Task AddGameAsync(GameDto game) => UpdateGameAsync(game);

    public Task UpdateGameAsync(GameDto game)
    {
        _games[game.Id] = JsonSerializer.Serialize(game);
        return Task.CompletedTask;
    }

    /// <summary>Simulates the cached copy being lost (Redis restart or expiry) between polls.</summary>
    public void Forget(int gameId) => _games.Remove(gameId);

    public Task<DateTime?> GetNextRunAsync(int gameId) => Task.FromResult<DateTime?>(null);
    public Task SetNextRunAsync(int gameId, DateTime nextRun) => Task.CompletedTask;
    public Task RemoveNextRunAsync(int gameId) => Task.CompletedTask;

    public Task<bool> TryClaimPreGameAlertAsync(int gameId) => Task.FromResult(_claims.Add($"pregame:{gameId}"));
    public Task ReleasePreGameAlertAsync(int gameId) => Task.FromResult(_claims.Remove($"pregame:{gameId}"));
    public Task<bool> TryClaimUserPicksNotificationAsync(Guid userId) => Task.FromResult(_claims.Add($"user:{userId}"));
    public Task ReleaseUserPicksNotificationAsync(Guid userId) => Task.FromResult(_claims.Remove($"user:{userId}"));
    public Task<bool> HasUserBeenNotifiedForGameAsync(Guid userId, int gameId) => Task.FromResult(_claims.Contains($"notified:{userId}:{gameId}"));

    public Task MarkUserNotifiedForGameAsync(Guid userId, int gameId, DateTime utcNow, DateTime gameStartUtc)
    {
        _claims.Add($"notified:{userId}:{gameId}");
        return Task.CompletedTask;
    }
}
