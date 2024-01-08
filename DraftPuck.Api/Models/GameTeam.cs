namespace DraftPuck.Api.Models;

public class GameTeam : Team
{
    public int Score { get; set; }
    public List<Player> Roster { get; set; } = new();
}
