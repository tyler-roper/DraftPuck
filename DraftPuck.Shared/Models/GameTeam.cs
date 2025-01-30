namespace DraftPuck.Shared.Models;

public class GameTeam : Team
{
    public int Score { get; set; }
    public List<Player> Roster { get; set; } = new();
    public List<TeamSituation> Situations { get; set; } = new();
    public int Strength { get; set; }
}
