namespace DraftPuck.Models.NhlApi
{
public class TimeZone
    {
        public string Id { get; set; } = null!;
        public int Offset { get; set; }
        public string Tz { get; set; } = null!;
    }
}