namespace Draftpuck.Nhl.Models
{
    public class NhlGamePlayByPlay : NhlGameBase
    {
        public int Period { get; set; }
        public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
        public List<NhlPlayerSummary> RosterSpots { get; set; } = null!;
        public int DisplayPeriod { get; set; }
        public List<NhlPlay> Plays { get; set; } = null!;
    }
}
