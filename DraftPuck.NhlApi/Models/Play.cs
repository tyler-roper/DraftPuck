namespace DraftPuck.NhlApi.Models
{
    public class Play
    {
        public int EventId { get; set; }
        public int Period { get; set; }
        public PeriodDescriptor PeriodDescriptor { get; set; } = null!;
        public string TimeInPeriod { get; set; } = null!;
        public string TimeRemaining { get; set; } = null!;
        public string SituationCode { get; set; } = null!;
        public string HomeTeamDefendingSide { get; set; } = null!;
        public int TypeCode { get; set; }
        public string TypeDescKey { get; set; } = null!;
        public int SortOrder { get; set; }
        public PlayDetails Details { get; set; } = null!;
    }
}
