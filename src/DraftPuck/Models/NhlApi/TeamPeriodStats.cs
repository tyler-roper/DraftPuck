namespace DraftPuck.Models.NhlApi
{
public class TeamPeriodStats
    {
        public int Goals { get; set; }
        public int ShotsOnGoal { get; set; }
        public string RinkSide { get; set; } = null!;
    }
}