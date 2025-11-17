namespace DraftPuck.Application.Features.Lobbies;

public class SendLobbyMessageCommand : IRequest
{
    public string Message { get; set; } = null!;
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}