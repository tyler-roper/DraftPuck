using DraftPuck.Application.Common.Exceptions;
using MediatR;

namespace DraftPuck.Application.Features.Lobbies.Picks;

public class RemovePickCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<RemovePickCommand>
{
    public async Task Handle(RemovePickCommand request, CancellationToken ct)
    {
        var pick = await dbContext.LobbyMemberPicks
            .Include(lmp => lmp.LobbyMember)
                .ThenInclude(lm => lm.Lobby)
            .FirstOrDefaultAsync(lmp => lmp.Id == request.PickId, ct);

        if (pick == null) return;

        var lobbyMember = pick.LobbyMember;
        var lobby = lobbyMember.Lobby;

        var isPickOwner = lobbyMember.UserId == request.UserId;
        var isLobbyCreator = lobby.CreatedBy == request.UserId;

        if (!isPickOwner && !isLobbyCreator)
        {
            var isAdmin = await dbContext.Users.AnyAsync(u => u.Id == request.UserId && u.IsAdmin, ct);
            if (!isAdmin) throw new UnauthorizedException("This user does not have permission to remove this pick.");
        }

        pick.IsActive = false;
        await dbContext.SaveChangesAsync(ct);
        await mediator.Publish(new PickRemovedNotification(new(lobby, lobbyMember, pick)), ct);
    }
}