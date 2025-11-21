namespace DraftPuck.Web.Features.Lobbies;

public class JoinLobbyRequestDto
{
    public string Name { get; set; } = null!;
    public bool IsBot { get; set; }
    public BotPickStyle? BotPickStyle { get; set; }
}
