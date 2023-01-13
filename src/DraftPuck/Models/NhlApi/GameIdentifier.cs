namespace DraftPuck.Models.NhlApi
{
    public class GameIdentifier
    {
        public long Pk { get; set; }
        public string Season { get; set; } = null!;
        public string Type { get; set; } = null!;
    }
}
