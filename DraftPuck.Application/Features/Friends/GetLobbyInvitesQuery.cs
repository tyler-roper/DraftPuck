namespace DraftPuck.Application.Features.Friends;

/// <summary>Who has a pending invite to a lobby the caller is in, so the share dialog can mark them.</summary>
public class GetLobbyInvitesQuery : IRequest<List<Guid>>
{
    public Guid UserId { get; set; }
    public string JoinCode { get; set; } = null!;
}
