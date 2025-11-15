using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies;

public class LeaveLobbyCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<LeaveLobbyCommand>
{
    public async Task Handle(LeaveLobbyCommand request, CancellationToken cancellationToken)
    {
        LobbyMemberEntity? newLobbyCreatorMember = null;

        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, cancellationToken)
            ?? throw new NotFoundException($"Lobby with code '{request.Code}' not found.");

        var memberToRemove = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == request.UserId);

        if (memberToRemove == null || memberToRemove.IsRemoved)
            return;

        if (memberToRemove.UserId == lobby.CreatedBy)
        {
            var newCreatorMember = lobby
                .LobbyMembers
                .Where(lm => lm.Id != memberToRemove.Id && !lm.IsBot && !lm.IsRemoved)
                .OrderBy(lm => lm.Joined)
                .FirstOrDefault();

            if (newCreatorMember != null)
            {
                lobby.CreatedBy = newCreatorMember.UserId;
                newLobbyCreatorMember = newCreatorMember;
            }
            else
            {
                await DeleteLobby(lobby, cancellationToken);
                return;
            }
        }

        memberToRemove.IsRemoved = true;
        foreach (var pick in memberToRemove.LobbyMemberPicks)
            pick.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        var userLeftPayload = new LobbyMemberEventPayload(lobby, memberToRemove);
        await mediator.Publish(new UserLeftNotification(userLeftPayload), cancellationToken);

        if (newLobbyCreatorMember != null)
        {
            var userPromotedPayload = new LobbyMemberEventPayload(lobby, newLobbyCreatorMember);
            await mediator.Publish(new UserPromotedNotification(userPromotedPayload), cancellationToken);
        }
    }

    private async Task DeleteLobby(LobbyEntity lobby, CancellationToken cancellationToken)
    {
        var lobbyEvents = dbContext.LobbyEvents.Where(le => le.LobbyId == lobby.Id);
        var lobbyMemberIds = lobby.LobbyMembers.Select(lm => lm.Id).ToList();
        var messages = dbContext.Messages.Where(m => lobbyMemberIds.Contains(m.LobbyMemberId));
        var memberPicks = lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks);
        var memberPickIds = memberPicks.Select(lmp => lmp.Id);
        var drinks = dbContext.Drinks.Where(d => memberPickIds.Contains(d.LobbyMemberPickId));

        dbContext.LobbyEvents.RemoveRange(lobbyEvents);
        dbContext.Messages.RemoveRange(messages);
        dbContext.Drinks.RemoveRange(drinks);
        dbContext.LobbyMemberPicks.RemoveRange(memberPicks);
        dbContext.LobbyMembers.RemoveRange(lobby.LobbyMembers);
        dbContext.Lobbies.Remove(lobby);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}