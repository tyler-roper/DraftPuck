namespace DraftPuck.Application.Features.Friends;

/// <summary>
/// Invites some of the caller's friends to a lobby the caller is in. Friends already in it, or already invited, are
/// skipped. Returns everyone with a pending invite to the lobby, so the caller can show who's been asked.
/// </summary>
public class SendLobbyInvitesCommand : IRequest<List<Guid>>
{
    public Guid UserId { get; set; }
    public string JoinCode { get; set; } = null!;
    public List<Guid> FriendIds { get; set; } = [];
}
