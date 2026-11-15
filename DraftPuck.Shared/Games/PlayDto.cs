using System.Text.Json.Serialization;

namespace DraftPuck.Shared.Games;

public class PlayDto
{
    public int Id { get; set; }
    /// <summary>
    /// When the play happened (UTC). The NHL doesn't send one, so it's set while processing each poll
    /// (<c>GameProcessingHelpers.SetPlayTimes</c>): the time of the poll that first saw the play, within one poll interval
    /// of it happening, or, for a play that was already there when we started watching the game, an estimate from the
    /// game clock.
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Reads games cached before plays had one time, when the time a poll first saw a play was kept here and
    /// <see cref="DateTime"/> held the estimate. Never written. The JSON has it after DateTime, so it wins when set.
    /// Cached games expire after a day: delete this a day after it ships.
    /// </summary>
    [JsonInclude]
    private DateTime? FirstSeenUtc
    {
        set { if (value is { } firstSeen) DateTime = firstSeen; }
    }
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
