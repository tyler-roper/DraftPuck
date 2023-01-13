namespace DraftPuck.Models.NhlApi
{
    public class PlayerStatsResponse
    {
        public string Copyright { get; set; } = null!;
        public List<Stat> Stats { get; set; } = null!;
    }
}
