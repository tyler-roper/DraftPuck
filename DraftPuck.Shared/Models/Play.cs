namespace DraftPuck.Shared.Models;

public class Play
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public int Period { get; set; }
    public PeriodType PeriodType { get; set; }
    public string TimeInPeriod { get; set; } = null!;
    public string TimeRemainingInPeriod { get; set; } = null!;
    public PlayType Type { get; set; }
    public int? PrimaryPlayerId { get; set; }
    public int? PrimaryTeamId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string? Penalty { get; set; }
}
