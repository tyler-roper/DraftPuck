namespace DraftPuck.Models.NhlApi
{
public class TeamSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Link { get; set; } = null!;
        public string? TriCode { get; set; }
    }
}