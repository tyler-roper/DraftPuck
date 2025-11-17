namespace DraftPuck.Shared.Games;

public class GameSummaryDto
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public GameType GameType { get; set; }
    public GameState GameState { get; set; }
    public TeamDto HomeTeam { get; set; } = new();
    public TeamDto AwayTeam { get; set; } = new();
    public int Period { get; set; }
    public PeriodType PeriodType { get; set; }
    public int MinutesRemainingInPeriod { get; set; }
    public int SecondsRemainingInPeriod { get; set; }
    public string TimeRemainingInPeriod => MinutesRemainingInPeriod > 0 || SecondsRemainingInPeriod > 0 ? $"{MinutesRemainingInPeriod:0}:{SecondsRemainingInPeriod:00}" : "End";
}
