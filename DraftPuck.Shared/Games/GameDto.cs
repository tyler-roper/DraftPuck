namespace DraftPuck.Shared.Games;

public class GameDto
{
    public int Id { get; set; }

    /// <summary>
    /// When this copy was produced by a poll. Clients get games both by fetching and by push, so they keep whichever
    /// copy is newest. Not from the NHL.
    /// </summary>
    public DateTime UpdatedUtc { get; set; }
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

    // Pick rules that depend on the game. Defined here, once, so the server enforces them and clients receive them as
    // facts with every fetch and push (they serialize like any property), instead of each re-implementing them.

    /// <summary>Picking for a game opens this long before puck drop.</summary>
    public static readonly TimeSpan PickWindowBeforeStart = TimeSpan.FromMinutes(30);

    /// <summary>When members can start picking. Picking stays open through the game, until it's final.</summary>
    public DateTime PickingOpensUtc => DateTime - PickWindowBeforeStart;

    /// <summary>No more picks once the game is final.</summary>
    public bool IsPickingClosed => GameState == GameState.Final;

    /// <summary>Members can remove their own picks until puck drop. (The host and site admins always can.)</summary>
    public bool CanMembersRemovePicks => GameState == GameState.Upcoming;

    public bool IsPickingOpen(DateTime nowUtc) => nowUtc >= PickingOpensUtc && !IsPickingClosed;

    /// <summary>
    /// Players who were in this game's lineup earlier but no longer are (e.g. scratched before puck drop). Not pickable;
    /// kept so lobby events that mention them can still show their names. Not from the NHL: carried forward each poll.
    /// </summary>
    public List<PlayerSummaryDto> FormerPlayers { get; set; } = [];
}
