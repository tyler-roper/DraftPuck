namespace DraftPuck.Shared.Models;

public class NewLobbyRequest
{
    public string Name { get; set; } = null!;
    public int PicksPerTeam { get; set; }
    public bool IsBotAutoPickingEnabled { get; set; } = false;
    public List<int> GameIds { get; set; } = new();
}