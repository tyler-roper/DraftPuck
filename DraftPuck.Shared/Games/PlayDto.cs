namespace DraftPuck.Shared.Games;

public class PlayDto
{
    public int Id { get; set; }
    /// <summary>Estimated from the game clock (the NHL doesn't send a wall-clock time), so it can be minutes off.</summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// When a poll first saw this play (real UTC, within one poll interval of it happening). Null when unknown: the play
    /// was already there on the first poll we have. Not from the NHL: carried forward each poll.
    /// </summary>
    public DateTime? FirstSeenUtc { get; set; }

    /// <summary>Best known time the play happened: when we first saw it, else the estimate.</summary>
    public DateTime HappenedUtc => FirstSeenUtc ?? DateTime;
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
