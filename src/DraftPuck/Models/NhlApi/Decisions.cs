namespace DraftPuck.Models.NhlApi
{
public class Decisions
    {
        public PlayerSummary Winner { get; set; } = null!;
        public PlayerSummary Loser { get; set; } = null!;
        public PlayerSummary FirstStar { get; set; } = null!;
        public PlayerSummary SecondStar { get; set; } = null!;
        public PlayerSummary ThirdStar { get; set; } = null!;
    }
}