namespace DraftPuck.Models.NhlApi
{
    public class PlayerSummary
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Link { get; set; } = null!;
    }
}