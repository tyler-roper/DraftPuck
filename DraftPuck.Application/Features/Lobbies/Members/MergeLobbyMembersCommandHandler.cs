using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Members;

/// <summary>
/// Swaps the two spots' logins, then removes the duplicate. The original spot keeps everything it had and now belongs
/// to the person's current login; the removed duplicate goes to the login they left behind. Nothing is deleted, so
/// drinks others gave the duplicate, or got from its picks, stay as they were (as when the host removes a member).
/// </summary>
public class MergeLobbyMembersCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<MergeLobbyMembersCommand>
{
    public async Task Handle(MergeLobbyMembersCommand request, CancellationToken ct)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct)
            ?? throw new NotFoundException($"Lobby with code '{request.Code}' not found.");

        if (lobby.CreatedBy != request.RequesterUserId)
            throw new ForbiddenException("Only the lobby host can merge members.");

        if (!lobby.IsActive)
            throw new BadRequestException("This lobby has ended.");

        if (request.OriginalMemberId == request.DuplicateMemberId)
            throw new BadRequestException("Choose two different members.");

        var original = CurrentMember(lobby, request.OriginalMemberId);
        var duplicate = CurrentMember(lobby, request.DuplicateMemberId);

        if (original.IsBot || duplicate.IsBot)
            throw new BadRequestException("Bots can't be merged.");

        // The host's own spot never moves: the host is the one doing this, so it isn't the spot they lost.
        if (original.UserId == lobby.CreatedBy)
            throw new BadRequestException("The host's spot can't be handed to someone else.");

        // Only a guest's spot can move to another login. An account's spot stays with the account (signing in on the
        // new device brings it back), so a host can't give a signed-in player's picks to someone else.
        var originalIsGuest = await dbContext.Users.AnyAsync(u => u.Id == original.UserId && u.IsGuest && !u.IsBot, ct);
        if (!originalIsGuest)
            throw new BadRequestException($"{original.Name} is signed in to an account, so their spot can't move. They can sign in on the new device instead.");

        (original.UserId, duplicate.UserId) = (duplicate.UserId, original.UserId);

        duplicate.IsRemoved = true;
        foreach (var pick in duplicate.LobbyMemberPicks)
            pick.IsActive = false;

        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new MembersMergedNotification(new(lobby, original, duplicate)), ct);
    }

    private static LobbyMemberEntity CurrentMember(LobbyEntity lobby, Guid memberId)
        => lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == memberId && !lm.IsRemoved)
            ?? throw new NotFoundException("That member isn't in the lobby.");
}
