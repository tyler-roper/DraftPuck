namespace DraftPuck.Application.Features.Lobbies;

public class JoinLobbyCommand : IRequest<LobbyDto>
{
    public string Name { get; set; } = null!;
    public bool IsBot { get; set; }
    public BotPickStyle? BotPickStyle { get; set; }
    public string Code { get; set; } = null!;
    public Guid? UserId { get; set; }
}