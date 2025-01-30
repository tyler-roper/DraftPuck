namespace DraftPuck.Shared.Models;

public class Schedule
{
    public DateTime Date { get; set; }
    public List<GameSummary> Games { get; set; } = new();
}
