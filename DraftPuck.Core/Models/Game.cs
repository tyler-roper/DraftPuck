namespace DraftPuck.Core.Models;

public class Game
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public GameType GameType { get; set; }
    public GameState GameState { get; set; }
    public GameTeam HomeTeam { get; set; } = new();
    public GameTeam AwayTeam { get; set; } = new();
    public List<Play> Plays { get; set; } = new();
    public int Period { get; set; }
    public PeriodType PeriodType { get; set; }
    public int MinutesRemainingInPeriod { get; set; }
    public int SecondsRemainingInPeriod { get; set; }
    public string TimeRemainingInPeriod => MinutesRemainingInPeriod > 0 || SecondsRemainingInPeriod > 0 ? $"{MinutesRemainingInPeriod:0}:{SecondsRemainingInPeriod:00}" : "End";
    public List<PeriodSummary> GoalsByPeriod { get; set; } = new();
    public List<PlayerSummary> PlayerSummaries { get; set; } = new();
}
