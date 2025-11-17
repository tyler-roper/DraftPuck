namespace DraftPuck.Shared.Games;

public class PeriodSummaryDto
{
    public int Number { get; set; }
    public PeriodType PeriodType { get; set; }
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
}
