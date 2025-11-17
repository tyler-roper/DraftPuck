using DraftPuck.Application.Features.Games;
using DraftPuck.Infrastructure.Nhl.Models;
using DraftPuck.Infrastructure.RateLimiting;
using System.Net.Http.Json;

namespace DraftPuck.Infrastructure.Nhl;

public class NhlClient(HttpClient httpClient, IMapper mapper, IApiRateLimiter rateLimiter) : INhlClient
{
    public async Task<ScheduleDto> GetScheduleAsync(DateTime date)
    {
        await rateLimiter.WaitForPermitAsync();
        var nhlSchedule = (await httpClient.GetFromJsonAsync<NhlSchedule>($"schedule/{date:yyyy-MM-dd}"))!;
        return mapper.Map<ScheduleDto>(nhlSchedule);
    }

    public async Task<GameDto> GetFullGameAsync(int gameId)
    {
        var playByPlay = await GetPlayByPlayAsync(gameId);
        var landing = await GetGameLandingAsync(gameId);

        var nhlFullGame = new NhlFullGame(playByPlay, landing);
        return mapper.Map<GameDto>(nhlFullGame);
    }

    public async Task<PlayerDto> GetPlayerAsync(int playerId)
    {
        await rateLimiter.WaitForPermitAsync();
        var nhlPlayer = await httpClient.GetFromJsonAsync<NhlPlayer>($"player/{playerId}/landing");
        return mapper.Map<PlayerDto>(nhlPlayer);
    }

    private async Task<NhlGameLanding> GetGameLandingAsync(int gameId)
    {
        await rateLimiter.WaitForPermitAsync();
        return (await httpClient.GetFromJsonAsync<NhlGameLanding>($"gamecenter/{gameId}/landing"))!;
    }

    private async Task<NhlGamePlayByPlay> GetPlayByPlayAsync(int gameId)
    {
        await rateLimiter.WaitForPermitAsync();
        return (await httpClient.GetFromJsonAsync<NhlGamePlayByPlay>($"gamecenter/{gameId}/play-by-play"))!;
    }
}
