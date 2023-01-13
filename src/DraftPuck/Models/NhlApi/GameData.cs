namespace DraftPuck.Models.NhlApi
{
    public class GameData
    {
        public GameIdentifier Game { get; set; } = null!;
        public DateTimes Datetime { get; set; } = null!;
        public Status Status { get; set; } = null!;
        public Teams Teams { get; set; } = null!;
        public Dictionary<string, Player> Players { get; set; } = null!;
        public VenueSummary Venue { get; set; } = null!;
    }
}
