namespace DraftPuck.Shared.Games;

public class GameDto
{
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public GameType GameType { get; set; }
    public GameState GameState { get; set; }
    public GameTeamDto HomeTeam { get; set; } = new();
    public GameTeamDto AwayTeam { get; set; } = new();
    public List<PlayDto> Plays { get; set; } = [];
    public int Period { get; set; }
    public PeriodType PeriodType { get; set; }
    public int MinutesRemainingInPeriod { get; set; }
    public int SecondsRemainingInPeriod { get; set; }
    public string TimeRemainingInPeriod => MinutesRemainingInPeriod > 0 || SecondsRemainingInPeriod > 0 ? $"{MinutesRemainingInPeriod:0}:{SecondsRemainingInPeriod:00}" : "End";
    public List<PeriodSummaryDto> GoalsByPeriod { get; set; } = [];
    public List<PlayerSummaryDto> PlayerSummaries { get; set; } = [];

    /// <summary>
    /// Players who were in this game's lineup earlier but no longer are (e.g. scratched before puck drop). Not pickable;
    /// kept so lobby events that mention them can still show their names. Not from the NHL: carried forward each poll.
    /// </summary>
    public List<PlayerSummaryDto> FormerPlayers { get; set; } = [];
}
