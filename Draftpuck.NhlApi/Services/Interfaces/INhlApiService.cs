using Draftpuck.NhlApi.Models;

namespace Draftpuck.NhlApi.Services.Interfaces;

public interface INhlApiService
{
    public Task<NhlSchedule> GetScheduleAsync(DateTime date);
    public Task<NhlGameLanding> GetGameLandingAsync(int gameId);
    public Task<NhlGamePlayByPlay> GetPlayByPlayAsync(int gameId);
    public Task<NhlPlayer> GetPlayerAsync(int playerId);
    public Task<NhlFullGame> GetFullGameAsync(int gameId);
}
