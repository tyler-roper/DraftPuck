namespace DraftPuck.Models.NhlApi
{
public class GameOfficial
    {
        public OfficialSummary Official { get; set; } = null!;
        public string OfficialType { get; set; } = null!;
    }
}