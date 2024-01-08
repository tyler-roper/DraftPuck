namespace DraftPuck.NhlApi.Models
{
    public class GameDate
    {
        public string Date { get; set; } = null!;
        public string DayAbbrev { get; set; } = null!;
        public int NumberOfGames { get; set; }
        public List<ScheduleGame> Games { get; set; } = new();
    }
}
