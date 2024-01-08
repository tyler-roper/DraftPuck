namespace DraftPuck.NhlApi.Models
{
    public class TeamSummary
    {
        public int Id { get; set; }
        public DefaultString PlaceName { get; set; } = null!;
        public string Abbrev { get; set; } = null!;
        public string Logo { get; set; } = null!;
        public string DarkLogo { get; set; } = null!;
        public int Score { get; set; }
    }
}
