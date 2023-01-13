namespace DraftPuck.Models.NhlApi
{
    public class Play
    {
        public PlayResult Result { get; set; } = null!;
        public PlayAbout About { get; set; } = null!;
        public Coordinates Coordinates { get; set; } = null!;
        public List<PlayPlayer>? Players { get; set; }
        public TeamSummary? Team { get; set; }
    }

    public class PlayPlayer
    {
        public PlayerSummary Player { get; set; } = null!;
        public string PlayerType { get; set; } = null!;
        public int? SeasonTotal { get; set; }
    }
}