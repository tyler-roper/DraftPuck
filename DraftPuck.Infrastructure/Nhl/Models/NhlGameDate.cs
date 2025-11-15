namespace DraftPuck.Infrastructure.Nhl.Models;

public class NhlGameDate
{
    public string Date { get; set; } = null!;
    public string DayAbbrev { get; set; } = null!;
    public int NumberOfGames { get; set; }
    public List<NhlScheduleGame> Games { get; set; } = [];
}
