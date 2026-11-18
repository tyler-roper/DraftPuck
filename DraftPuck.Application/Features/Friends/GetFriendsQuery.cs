namespace DraftPuck.Application.Features.Friends;

public class GetFriendsQuery : IRequest<FriendsDto>
{
    public Guid UserId { get; set; }
}
