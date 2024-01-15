namespace Draftpuck.NhlApi.Models;

public class NhlGameBase
{
    public int Id { get; set; }
    public int Season { get; set; }
    public int GameType { get; set; }
    public string GameDate { get; set; } = null!;
    public NhlDefaultString Venue { get; set; } = null!;
    public DateTime StartTimeUTC { get; set; }
    public string EasternUTCOffset { get; set; } = null!;
    public string VenueUTCOffset { get; set; } = null!;
    public List<NhlTvBroadcast> TvBroadcasts { get; set; } = null!;
    public string GameState { get; set; } = null!;
    public string GameScheduleState { get; set; } = null!;
    public NhlTeamSummary AwayTeam { get; set; } = null!;
    public NhlTeamSummary HomeTeam { get; set; } = null!;
    public NhlClock Clock { get; set; } = null!;
}
