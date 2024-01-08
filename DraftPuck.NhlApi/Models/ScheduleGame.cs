namespace DraftPuck.NhlApi.Models
{
    public class ScheduleGame
    {
        public int Id { get; set; }
        public int Season { get; set; }
        public int GameType { get; set; }
        public DateTime StartTimeUTC { get; set; }
        public string GameState { get; set; } = null!;
        public string GameScheduleState { get; set; } = null!;
        public TeamSummary AwayTeam { get; set; } = null!;
        public TeamSummary HomeTeam { get; set; } = null!;
        public PeriodDescriptor PeriodDescriptor { get; set; } = null!;
    }
}
