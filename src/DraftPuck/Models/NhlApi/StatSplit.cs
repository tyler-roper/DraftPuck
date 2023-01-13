namespace DraftPuck.Models.NhlApi
{
    public class StatSplit
    {
        public string season { get; set; } = null!;
        public PlayerSeasonStats Stat { get; set; } = null!;
    }
}