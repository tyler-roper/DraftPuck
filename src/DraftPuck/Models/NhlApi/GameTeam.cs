namespace DraftPuck.Models.NhlApi
{
public class GameTeam
    {
        public int Score { get; set; }
        public TeamSummary Team { get; set; } = null!;
        public LeagueRecord LeagueRecord { get; set; } = null!;
    }
}