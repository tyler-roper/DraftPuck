using Draftpuck.Nhl.Models;

namespace Draftpuck.Nhl.Services.Interfaces
{
    public interface INhlService
    {
        public Task<Schedule> GetScheduleByDateAsync(DateTime date);
        public Task<Player> GetPlayerAsync(int playerId);
        public Task<Game> GetGameAsync(int gameId);
    }
}
