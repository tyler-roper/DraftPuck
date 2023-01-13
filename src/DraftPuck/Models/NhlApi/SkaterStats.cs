namespace DraftPuck.Models.NhlApi
{
public class SkaterStats
    {
        public string TimeOnIce { get; set; } = null!;
        public int Assists { get; set; }
        public int Goals { get; set; }
        public int Shots { get; set; }
        public int Hits { get; set; }
        public int PowerPlayGoals { get; set; }
        public int PowerPlayAssists { get; set; }
        public int PenaltyMinutes { get; set; }
        public decimal FaceOffPct { get; set; }
        public int FaceOffWins { get; set; }
        public int FaceoffTaken { get; set; }
        public int Takeaways { get; set; }
        public int Giveaways { get; set; }
        public int ShortHandedGoals { get; set; }
        public int ShortHandedAssists { get; set; }
        public int Blocked { get; set; }
        public int PlusMinus { get; set; }
        public string EvenTimeOnIce { get; set; } = null!;
        public string PowerPlayTimeOnIce { get; set; } = null!;
        public string ShortHandedTimeOnIce { get; set; } = null!;
    }
}