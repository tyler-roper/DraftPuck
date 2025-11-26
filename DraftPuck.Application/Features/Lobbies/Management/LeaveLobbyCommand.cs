namespace DraftPuck.Application.Features.Lobbies.Management;

public class LeaveLobbyCommand : IRequest
{
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}