using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class JoinLobbyCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<JoinLobbyCommand, LobbyDto>
{
    public async Task<LobbyDto> Handle(JoinLobbyCommand request, CancellationToken ct)
    {
        if (request.IsBot)
            request.UserId = Guid.NewGuid();
        else if (request.UserId == null)
            throw new UnauthorizedException("You must be logged in to join.");

        if (request.IsBot)
        {
            var botUser = await dbContext.Users.FindAsync([request.UserId.Value], cancellationToken: ct);

            if (botUser == null)
                dbContext.Users.Add(new UserEntity { Id = request.UserId.Value, IsBot = true });
        }

        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct)
            ?? throw new NotFoundException("Lobby not found.");

        var existingLobbyMember = await dbContext.LobbyMembers
            .Include(lm => lm.LobbyMemberPicks)
            .FirstOrDefaultAsync(lm => lm.LobbyId == lobby.Id && lm.UserId == request.UserId.Value, ct);

        if (existingLobbyMember == null)
        {
            var lobbyMember = new LobbyMemberEntity
            {
                LobbyId = lobby.Id,
                UserId = request.UserId.Value,
                Name = request.Name,
                IsBot = request.IsBot,
                BotPickStyle = request.BotPickStyle
            };
            dbContext.LobbyMembers.Add(lobbyMember);
            await dbContext.SaveChangesAsync(ct);

            await mediator.Publish(new UserJoinedLobbyNotification(new(lobby, lobbyMember)), ct);
        }
        else if (!existingLobbyMember.IsRemoved)
        {
            if (existingLobbyMember.Name != request.Name)
            {
                var previousName = existingLobbyMember.Name;
                existingLobbyMember.Name = request.Name;
                await dbContext.SaveChangesAsync(ct);

                await mediator.Publish(new UserNameChangedNotification(new(lobby, existingLobbyMember, previousName)), ct);
            }
        }
        else // Rejoining after removal
        {
            var previousName = existingLobbyMember.Name;
            existingLobbyMember.Name = request.Name;
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