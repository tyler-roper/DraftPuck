namespace DraftPuck.Application.Features.Friends;

/// <summary>
/// Asks another player to be friends, or accepts their request if they already asked. Returns where the two now stand.
/// </summary>
public class AddFriendCommand : IRequest<FriendStatus>
{
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
}
