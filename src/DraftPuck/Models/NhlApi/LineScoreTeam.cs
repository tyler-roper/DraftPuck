namespace DraftPuck.Models.NhlApi
{
public class LineScoreTeam
    {
        public ExtendedTeamSummary Team { get; set; } = null!;
        public int Goals { get; set; }
        public int ShotsOnGoal { get; set; }
        public bool GoaliePulled { get; set; }
        public int NumSkaters { get; set; }
        public bool PowerPlay { get; set; }
    }
}