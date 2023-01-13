using DraftPuck.Models.NhlApi;
using Newtonsoft.Json.Linq;

namespace DraftPuck.Services
{
    public class NhlApiService : INhlApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NhlApiService> _logger;

        public NhlApiService(HttpClient httpClient, ILogger<NhlApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Schedule> GetScheduleAsync(bool withScoringPlays = false)
        {
            var path = "schedule";
            if (withScoringPlays) path += "?expand=schedule.scoringplays";
            return await _httpClient.GetFromJsonAsync<Schedule>(path);
        }

        public async Task<Schedule> GetScheduleAsync(DateTime date, bool withScoringPlays = false)
        {
            var path = $"schedule?startDate={date:yyyy-MM-dd}&endDate={date:yyyy-MM-dd}";
            if (withScoringPlays) path += "&expand=schedule.scoringplays";
            return await _httpClient.GetFromJsonAsync<Schedule>(path);
        }

        public async Task<Player> GetPlayerAsync(long playerId)
            => await _httpClient.GetFromJsonAsync<Player>($"people/{playerId}");

        public async Task<LiveGame> GetGameAsync(long gamePk)
            => await _httpClient.GetFromJsonAsync<LiveGame>($"game/{gamePk}/feed/live?cb={Guid.NewGuid()}");

        public async Task<JToken> GetPatchAsync(long gamePk, string startTimecode)
        {
            var str = await _httpClient.GetStringAsync($"game/{gamePk}/feed/live/diffPatch?startTimecode={startTimecode}&cb=${Guid.NewGuid()}");
            return JToken.Parse(str);
        }

        public async Task<PlayerStatsResponse> GetPlayerStatsAsync(long playerId)
            => await _httpClient.GetFromJsonAsync<PlayerStatsResponse>($"/people/{playerId}/stats?stats=statsSingleSeason");

        public async Task<LineScore> GetLinescoreForGameAsync(long gamePk)
            => await _httpClient.GetFromJsonAsync<LineScore>($"game/{gamePk}/linescore");
    }
}
