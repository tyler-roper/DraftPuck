namespace DraftPuck.Models.NhlApi
{
public class LineScore
    {
        public int CurrentPeriod { get; set; }
        public string CurrentPeriodOrdinal { get; set; } = null!;
        public string CurrentPeriodTimeRemaining { get; set; } = null!;
        public List<Period> Periods { get; set; } = null!;
        public ShootoutInfo ShootoutInfo { get; set; } = null!;
        public LineScoreTeams Teams { get; set; } = null!;
        public string PowerPlayStrength { get; set; } = null!;
        public bool HasShootout { get; set; }
        public IntermissionInfo IntermissionInfo { get; set; } = null!;
        public PowerPlayInfo PowerPlayInfo { get; set; } = null!;
    }
}