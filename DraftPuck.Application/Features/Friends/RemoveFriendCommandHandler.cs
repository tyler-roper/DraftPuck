namespace DraftPuck.Application.Features.Friends;

public class RemoveFriendCommandHandler(IDbContext dbContext) : IRequestHandler<RemoveFriendCommand>
{
    public async Task Handle(RemoveFriendCommand request, CancellationToken ct)
    {
        var friendship = await dbContext.Friendships.BetweenAsync(request.UserId, request.FriendId, ct);
        if (friendship == null) return;

        dbContext.Friendships.Remove(friendship);
        await dbContext.SaveChangesAsync(ct);
    }
}
