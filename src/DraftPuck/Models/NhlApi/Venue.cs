namespace DraftPuck.Models.NhlApi
{
public class Venue
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Link { get; set; } = null!;
        public string City { get; set; } = null!;
        public TimeZone TimeZone { get; set; } = null!;
    }
}