using DraftPuck.Application.Features.Friends;

namespace DraftPuck.Web.Features.Friends;

/// <summary>The signed-in player's friends and lobby invites. Guests have none: adding or inviting is refused.</summary>
public class FriendsController(IMediator mediator) : BaseController()
{
    /// <summary>Friends, friend requests both ways, and pending lobby invites.</summary>
    [HttpGet]
    public async Task<ActionResult<FriendsDto>> GetFriends() =>
        Ok(await mediator.Send(new GetFriendsQuery { UserId = CurrentUserId }));

    /// <summary>Sends a friend request, or accepts theirs. Returns where the two now stand.</summary>
    [HttpPost("{userId:guid}")]
    public async Task<ActionResult<FriendStatus>> AddFriend(Guid userId) =>
        Ok(await mediator.Send(new AddFriendCommand { UserId = CurrentUserId, FriendId = userId }));

    /// <summary>Unfriends, or declines or cancels a request.</summary>
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid userId)
    {
        await mediator.Send(new RemoveFriendCommand { UserId = CurrentUserId, FriendId = userId });
        return NoContent();
    }

    /// <summary>Who has a pending invite to a lobby the caller is in.</summary>
    [HttpGet("invites/{joinCode}")]
    public async Task<ActionResult<List<Guid>>> GetLobbyInvites(string joinCode) =>
        Ok(await mediator.Send(new GetLobbyInvitesQuery { UserId = CurrentUserId, JoinCode = joinCode }));

    /// <summary>Invites friends to a lobby the caller is in. Returns everyone now invited to it.</summary>
    [HttpPost("invites/{joinCode}")]
    public async Task<ActionResult<List<Guid>>> SendLobbyInvites(string joinCode, SendLobbyInvitesRequestDto request) =>
        Ok(await mediator.Send(new SendLobbyInvitesCommand { UserId = CurrentUserId, JoinCode = joinCode, FriendIds = request.FriendIds }));

    /// <summary>Dismisses the caller's invite to a lobby.</summary>
    [HttpDelete("invites/{joinCode}")]
    public async Task<IActionResult> DismissLobbyInvite(string joinCode)
    {
        await mediator.Send(new DismissLobbyInviteCommand { UserId = CurrentUserId, JoinCode = joinCode });
        return NoContent();
    }
}
