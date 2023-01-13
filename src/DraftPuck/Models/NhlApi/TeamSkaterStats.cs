namespace DraftPuck.Models.NhlApi
{
public class TeamSkaterStats
    {
        public int Goals { get; set; }
        public int Pim { get; set; }
        public int Shots { get; set; }
        public string PowerPlayPercentage { get; set; } = null!;
        public decimal PowerPlayGoals { get; set; }
        public decimal PowerPlayOpportunities { get; set; }
        public string FaceOffWinPercentage { get; set; } = null!;
        public int Blocked { get; set; }
        public int Takeaways { get; set; }
        public int Giveaways { get; set; }
        public int Hits { get; set; }
    }
}