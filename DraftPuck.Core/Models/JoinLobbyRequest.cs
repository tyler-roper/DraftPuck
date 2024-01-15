namespace DraftPuck.Core.Models;

public class JoinLobbyRequest
{
    public string Name { get; set; } = null!;
    public bool IsBot { get; set; } = false;
    public BotPickStyle? BotPickStyle { get; set; }
}