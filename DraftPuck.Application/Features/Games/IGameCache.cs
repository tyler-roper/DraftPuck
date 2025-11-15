namespace DraftPuck.Application.Features.Games;

public interface IGameCache
{
    Task<GameDto?> GetGameByIdAsync(int gameId);
    Task<List<GameDto>> GetAllGamesAsync();
    Task RemoveGameAsync(int gameId);
    Task RemoveGameAsync(GameDto game);
    Task AddGameAsync(GameDto game);
    Task UpdateGameAsync(GameDto game);
    Task<DateTime?> GetNextRunAsync(int gameId);
    Task SetNextRunAsync(int gameId, DateTime nextRun);
    Task RemoveNextRunAsync(int gameId);
    Task<bool> HasPreGameAlertTriggeredAsync(int gameId);
    Task SetPreGameAlertTriggeredAsync(int gameId);
    Task<bool> HasUserBeenNotifiedRecentlyAsync(Guid userId);
    Task MarkUserAsNotifiedAsync(Guid userId);
    Task<bool> HasUserBeenNotifiedForGameAsync(Guid userId, int gameId);
    Task MarkUserNotifiedForGameAsync(Guid userId, int gameId, DateTime utcNow, DateTime gameStartUtc);
}
