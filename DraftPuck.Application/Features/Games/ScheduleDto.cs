namespace DraftPuck.Application.Features.Games;

public class ScheduleDto
{
    public DateTime Date { get; set; }
    public List<GameSummaryDto> Games { get; set; } = [];
}
