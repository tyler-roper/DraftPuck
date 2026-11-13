using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;
using MediatR;

namespace DraftPuck.Application.Features.Lobbies.Picks;

public class RemovePickCommandHandler(IDbContext dbContext, IMediator mediator, IGameCache gameCache) : IRequestHandler<RemovePickCommand>
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

        if (!request.IsSystemRemoval)
            await EnsureCanRemoveAsync(request.UserId, lobby, lobbyMember, pick, ct);

        pick.IsActive = false;
        await dbContext.SaveChangesAsync(ct);
        await mediator.Publish(new PickRemovedNotification(new(lobby, lobbyMember, pick)), ct);
    }

    /// <summary>
    /// The host and site admins can remove any pick. A member can remove their own pick only before the game starts.
    /// </summary>
    private async Task EnsureCanRemoveAsync(Guid userId, LobbyEntity lobby, LobbyMemberEntity pickOwner, LobbyMemberPickEntity pick, CancellationToken ct)
    {
        if (lobby.CreatedBy == userId)
            return;

        if (await dbContext.Users.AnyAsync(u => u.Id == userId && u.IsAdmin, ct))
            return;

        if (pickOwner.UserId != userId)
            throw new ForbiddenException("This user does not have permission to remove this pick.");

        var game = await gameCache.GetGameByIdAsync(pick.GameId);
        if (game != null && game.GameState != GameState.Upcoming)
            throw new ForbiddenException("Picks can't be removed after the game has started.");
    }
}