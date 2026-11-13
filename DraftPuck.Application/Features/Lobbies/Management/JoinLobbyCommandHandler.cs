using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Lobbies.Members;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class JoinLobbyCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<JoinLobbyCommand, LobbyDto>
{
    public async Task<LobbyDto> Handle(JoinLobbyCommand request, CancellationToken ct)
    {
        if (request.UserId == null)
            throw new UnauthorizedException("You must be logged in to join.");

        var name = LobbyMemberNames.Normalize(request.Name);

        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct)
            ?? throw new NotFoundException("Lobby not found.");

        // Bots are added by the host on the lobby's behalf; each one gets its own new bot user.
        var memberUserId = request.UserId.Value;
        if (request.IsBot)
        {
            if (lobby.CreatedBy != request.UserId)
                throw new ForbiddenException("Only the lobby host can add bots.");

            if (request.BotPickStyle == null)
                throw new BadRequestException("Bots require a pick style.");

            memberUserId = Guid.NewGuid();
            dbContext.Users.Add(new UserEntity { Id = memberUserId, IsBot = true });
        }

        var existingLobbyMember = await dbContext.LobbyMembers
            .Include(lm => lm.LobbyMemberPicks)
            .FirstOrDefaultAsync(lm => lm.LobbyId == lobby.Id && lm.UserId == memberUserId, ct);

        if (existingLobbyMember == null)
        {
            var lobbyMember = new LobbyMemberEntity
            {
                LobbyId = lobby.Id,
                UserId = memberUserId,
                Name = name,
                IsBot = request.IsBot,
                BotPickStyle = request.BotPickStyle
            };
            dbContext.LobbyMembers.Add(lobbyMember);
            await dbContext.SaveChangesAsync(ct);

            await mediator.Publish(new UserJoinedLobbyNotification(new(lobby, lobbyMember)), ct);
        }
        else if (!existingLobbyMember.IsRemoved)
        {
            if (existingLobbyMember.Name != name)
            {
                var previousName = existingLobbyMember.Name;
                existingLobbyMember.Name = name;
                await dbContext.SaveChangesAsync(ct);

                await mediator.Publish(new UserNameChangedNotification(new(lobby, existingLobbyMember, previousName)), ct);
            }
        }
        else // Rejoining after removal
        {
            var previousName = existingLobbyMember.Name;
            existingLobbyMember.Name = name;
            existingLobbyMember.IsRemoved = false;

            var activePicks = lobby.LobbyMembers
                .SelectMany(lm => lm.LobbyMemberPicks)
                .Where(lmp => lmp.IsActive)
                .Select(lmp => lmp.PlayerId)
                .ToHashSet();

            foreach (var pick in existingLobbyMember.LobbyMemberPicks)
            {
                if (!activePicks.Contains(pick.PlayerId))
                {
                    pick.IsActive = true;
                }
            }

            await dbContext.SaveChangesAsync(ct);
            await mediator.Publish(new UserRejoinedLobbyNotification(new(lobby, existingLobbyMember)), ct);

            if (previousName != existingLobbyMember.Name)
            {
                await mediator.Publish(new UserNameChangedNotification(new(lobby, existingLobbyMember, previousName)), ct);
            }
        }

        return mapper.Map<LobbyDto>(lobby);
    }
}