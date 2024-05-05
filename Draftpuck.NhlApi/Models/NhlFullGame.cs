namespace Draftpuck.NhlApi.Models;

public class NhlFullGame : NhlGameBase
{
    public NhlFullGame() { }

    public NhlFullGame(NhlGamePlayByPlay playByPlay, NhlGameLanding landing)
    {
        Id = playByPlay.Id;
        Season = playByPlay.Season;
        GameType = playByPlay.GameType;
        GameDate = playByPlay.GameDate;
        Venue = playByPlay.Venue;
        StartTimeUTC = playByPlay.StartTimeUTC;
        EasternUTCOffset = playByPlay.EasternUTCOffset;
        VenueUTCOffset = playByPlay.VenueUTCOffset;
        TvBroadcasts = playByPlay.TvBroadcasts;
        GameState = playByPlay.GameState;
        GameScheduleState = playByPlay.GameScheduleState;
        AwayTeam = landing.AwayTeam;
        HomeTeam = landing.HomeTeam;
        Clock = playByPlay.Clock;
        PeriodDescriptor = playByPlay.PeriodDescriptor;
        RosterSpots = playByPlay.RosterSpots;
        DisplayPeriod = playByPlay.DisplayPeriod;
        Plays = playByPlay.Plays;
        Summary = landing.Summary;
        Situation = landing.Situation;
    }

    public NhlPeriodDescriptor PeriodDescriptor { get; set; } = null!;
    public List<NhlPlayerSummary> RosterSpots { get; set; } = null!;
    public int DisplayPeriod { get; set; }
    public List<NhlPlay> Plays { get; set; } = null!;
    public NhlGameSummary Summary { get; set; } = null!;
    public NhlSituation Situation { get; set; } = null!;
}
