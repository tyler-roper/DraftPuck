namespace DraftPuck.Application.Features.Lobbies;

public class LeaveLobbyCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}