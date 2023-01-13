namespace DraftPuck.Models.NhlApi
{
public class ScheduleDate
    {
        public string Date { get; set; } = null!;
        public int TotalItems { get; set; }
        public int TotalEvents { get; set; }
        public int TotalGames { get; set; }
        public int TotalMatches { get; set; }
        public List<GameSummary> Games { get; set; } = null!;
    }
}