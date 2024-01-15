using Draftpuck.NhlApi.Models;
using Draftpuck.NhlApi.Services.Interfaces;
using System.Net.Http.Json;

namespace Draftpuck.NhlApi.Services;

public class NhlApiService : INhlApiService
{
    private readonly HttpClient _httpClient;

    public NhlApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NhlSchedule> GetScheduleAsync(DateTime date)
    {
        return (await _httpClient.GetFromJsonAsync<NhlSchedule>($"schedule/{date:yyyy-MM-dd}"))!;
    }

    public async Task<NhlGameLanding> GetGameLandingAsync(int gameId)
    {
        return (await _httpClient.GetFromJsonAsync<NhlGameLanding>($"gamecenter/{gameId}/landing"))!;
    }

    public async Task<NhlGamePlayByPlay> GetPlayByPlayAsync(int gameId)
    {
        return (await _httpClient.GetFromJsonAsync<NhlGamePlayByPlay>($"gamecenter/{gameId}/play-by-play"))!;
    }

    public async Task<NhlFullGame> GetFullGameAsync(int gameId)
    {
        Task<NhlGamePlayByPlay> playByPlayTask = GetPlayByPlayAsync(gameId);
        Task<NhlGameLanding> landingTask = GetGameLandingAsync(gameId);
        await Task.WhenAll(playByPlayTask, landingTask);

        NhlGamePlayByPlay playByPlay = playByPlayTask.Result;
        NhlGameLanding landing = landingTask.Result;

        return new NhlFullGame(playByPlay, landing);
    }

    public async Task<NhlPlayer> GetPlayerAsync(int playerId)
    {
        return (await _httpClient.GetFromJsonAsync<NhlPlayer>($"player/{playerId}/landing"))!;
    }
}
