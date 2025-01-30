using DraftPuck.Shared.Models;

namespace DraftPuck.Core.Services.Interfaces;

public interface INhlService
{
    public Task<Schedule> GetScheduleByDateAsync(DateTime date);
    public Task<Player> GetPlayerAsync(int playerId);
    public Task<Game> GetGameAsync(int gameId);
}
