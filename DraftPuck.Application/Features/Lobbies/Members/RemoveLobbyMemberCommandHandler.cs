using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Members;

public class RemoveLobbyMemberCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<RemoveLobbyMemberCommand>
{
    public async Task Handle(RemoveLobbyMemberCommand request, CancellationToken cancellationToken)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, cancellationToken)
            ?? throw new NotFoundException($"Lobby with code '{request.Code}' not found.");

        if (lobby.CreatedBy != request.RequesterUserId)
            throw new UnauthorizedException("Only the lobby host can remove members.");

        var memberToRemove = lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == request.LobbyMemberId);

        if (memberToRemove == null)
            return;

        if (memberToRemove.UserId == lobby.CreatedBy)
            throw new BadRequestException("The lobby host cannot be removed.");

        if (memberToRemove.IsRemoved)
            return;

        memberToRemove.IsRemoved = true;
        foreach (var pick in memberToRemove.LobbyMemberPicks)
            pick.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        var payload = new LobbyMemberEventPayload(lobby, memberToRemove);
        await mediator.Publish(new UserRemovedNotification(payload), cancellationToken);
    }
}