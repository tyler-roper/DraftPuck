namespace DraftPuck.Models.NhlApi
{
public class BoxScore
    {
        public BoxScoreTeams Teams { get; set; } = null!;
        public List<GameOfficial> Officials { get; set; } = null!;
    }
}