using DraftPuck.Models.NhlApi;
using Newtonsoft.Json.Linq;

namespace DraftPuck.Services.Interfaces
{
    public interface INhlApiService
    {
        public Task<Schedule> GetScheduleAsync(bool withScoringPlays = false);

        public Task<Schedule> GetScheduleAsync(DateTime date, bool withScoringPlays = false);

        public Task<Player> GetPlayerAsync(long playerId);

        public Task<LiveGame> GetGameAsync(long gamePk);

        public Task<JToken> GetPatchAsync(long gamePk, string startTimecode);

        public Task<PlayerStatsResponse> GetPlayerStatsAsync(long playerId);

        public Task<LineScore> GetLinescoreForGameAsync(long gamePk);
    }
}
