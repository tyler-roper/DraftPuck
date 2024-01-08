namespace DraftPuck.NhlApi.Models
{
    public class Game
    {
        public int Id { get; set; }
        public int Season { get; set; }
        public int GameType { get; set; }
        public string GameDate { get; set; } = null!;
        public DefaultString Venue { get; set; } = null!;
        public DateTime StartTimeUTC { get; set; }
        public string EasternUTCOffset { get; set; } = null!;
        public string VenueUTCOffset { get; set; } = null!;
        public List<TvBroadcast> TvBroadcasts { get; set; } = null!;
        public string GameState { get; set; } = null!;
        public string GameScheduleState { get; set; } = null!;
        public int Period { get; set; }
        public PeriodDescriptor PeriodDescriptor { get; set; } = null!;
        public TeamSummary AwayTeam { get; set; } = null!;
        public TeamSummary HomeTeam { get; set; } = null!;
        public Clock Clock { get; set; } = null!;
        public Situation Situation { get; set; } = null!;
        public List<Player> RosterSpots { get; set; } = null!;
        public int DisplayPeriod { get; set; }
        public List<Play> Plays { get; set; } = null!;
    }
}
