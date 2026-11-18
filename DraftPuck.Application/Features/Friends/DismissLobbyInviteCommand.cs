namespace DraftPuck.Application.Features.Friends;

/// <summary>Removes the caller's invite to a lobby, without joining it. The sender isn't told.</summary>
public class DismissLobbyInviteCommand : IRequest
{
    public Guid UserId { get; set; }
    public string JoinCode { get; set; } = null!;
}
