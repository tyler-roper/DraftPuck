namespace DraftPuck.Models.NhlApi
{
public class BoxScorePlayer
    {
        public PlayerPerson Person { get; set; } = null!;
        public string JerseyNumber { get; set; } = null!;
        public Position Position { get; set; } = null!;
        public BoxScorePlayerStats Stats { get; set; } = null!;
    }
}