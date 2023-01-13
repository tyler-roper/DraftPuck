namespace DraftPuck.Models.NhlApi
{
    public class GameSummary
    {
        public long GamePk { get; set; }
        public string Link { get; set; } = null!;
        public string GameType { get; set; } = null!;
        public string Season { get; set; } = null!;
        public DateTime GameDate { get; set; }
        public Status Status { get; set; } = null!;
        public GameTeams Teams { get; set; } = null!;
        public VenueSummary Venue { get; set; } = null!;
        public Content Content { get; set; } = null!;
    }
}