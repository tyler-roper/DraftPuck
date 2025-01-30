using DraftPuck.Infrastructure.Nhl.Models;
using DraftPuck.Infrastructure.Nhl.Services.Interfaces;
using System.Net.Http.Json;

namespace DraftPuck.Infrastructure.Nhl.Services;

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
        var playByPlayTask = GetPlayByPlayAsync(gameId);
        var landingTask = GetGameLandingAsync(gameId);
        await Task.WhenAll(playByPlayTask, landingTask);

        var playByPlay = playByPlayTask.Result;
        var landing = landingTask.Result;

        return new NhlFullGame(playByPlay, landing);
    }

    public async Task<NhlPlayer> GetPlayerAsync(int playerId)
    {
        return (await _httpClient.GetFromJsonAsync<NhlPlayer>($"player/{playerId}/landing"))!;
    }
}
