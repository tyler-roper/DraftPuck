namespace DraftPuck.Application.Features.Friends;

/// <summary>Unfriends another player, or declines or cancels a request between the two. Nobody is told.</summary>
public class RemoveFriendCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
}
