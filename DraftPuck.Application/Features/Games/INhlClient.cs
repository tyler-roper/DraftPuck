namespace DraftPuck.Application.Features.Games;

public interface INhlClient
{
    Task<ScheduleDto> GetScheduleAsync(DateTime date);
    Task<PlayerDto> GetPlayerAsync(int playerId);
    Task<GameDto> GetFullGameAsync(int gameId);
}
