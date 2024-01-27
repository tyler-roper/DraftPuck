namespace Draftpuck.NhlApi.Models;

public class NhlPlay
{
    public int EventId { get; set; }
    public int Period { get; set; }
    public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
    public string TimeInPeriod { get; set; } = null!;
    public string TimeRemaining { get; set; } = null!;
    public string SituationCode { get; set; } = null!;
    public string HomeTeamDefendingSide { get; set; } = null!;
    public int TypeCode { get; set; }
    public string TypeDescKey { get; set; } = null!;
    public int SortOrder { get; set; }
    public NhlPlayDetails Details { get; set; } = null!;
}
