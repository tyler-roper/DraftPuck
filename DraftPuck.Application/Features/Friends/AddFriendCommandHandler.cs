using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Domain.Friends;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Friends;

public class AddFriendCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<AddFriendCommand, FriendStatus>
{
    public async Task<FriendStatus> Handle(AddFriendCommand request, CancellationToken ct)
    {
        if (request.UserId == request.FriendId)
            throw new ValidationException("You can't add yourself as a friend.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException("User not found.");
        if (!FriendRules.CanHaveFriends(user))
            throw new ForbiddenException("Sign up to add friends.");

        var friend = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.FriendId, ct);
        if (friend == null || !FriendRules.CanHaveFriends(friend))
            throw new NotFoundException("That player can't be added as a friend.");

        var friendship = await dbContext.Friendships.BetweenAsync(user.Id, friend.Id, ct);

        if (friendship == null)
        {
            dbContext.Friendships.Add(new FriendshipEntity
            {
                Id = Guid.NewGuid(),
                RequesterId = user.Id,
                AddresseeId = friend.Id,
                Created = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(ct);
            await mediator.Publish(new FriendRequestedNotification(user, friend), ct);
            return FriendStatus.RequestSent;
        }

        if (friendship.IsAccepted)
            return FriendStatus.Friends;

        // Asking again doesn't push them again.
        if (friendship.RequesterId == user.Id)
            return FriendStatus.RequestSent;

        friendship.Accepted = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        await mediator.Publish(new FriendRequestAcceptedNotification(friend, user), ct);
        return FriendStatus.Friends;
    }
}
