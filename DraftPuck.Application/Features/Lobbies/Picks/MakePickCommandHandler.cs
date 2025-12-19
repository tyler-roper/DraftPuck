using DraftPuck.Application.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Picks;

public class MakePickCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<MakePickCommand, LobbyMemberPickDto>
{
    public async Task<LobbyMemberPickDto> Handle(MakePickCommand request, CancellationToken ct)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct)
            ?? throw new NotFoundException("Lobby not found.");

        var lobbyMember = (request.LobbyMemberId == null
            ? lobby.LobbyMembers.SingleOrDefault(lm => lm.UserId == request.UserId)
            : lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == request.LobbyMemberId))
            ?? throw new NotFoundException("User not found in lobby.");

        var lobbyMemberIds = lobby.LobbyMembers.Select(lm => lm.Id).ToList();
        var playerAlreadyPicked = await dbContext.LobbyMemberPicks
            .AnyAsync(p => p.IsActive
                && lobbyMemberIds.Contains(p.LobbyMemberId)
                && p.PlayerId == request.PlayerId
                && p.GameId == request.GameId, ct);

        if (playerAlreadyPicked)
            throw new ValidationException("Player already picked.");

        var pick = new LobbyMemberPickEntity
        {
            LobbyMemberId = lobbyMember.Id,
            PlayerId = request.PlayerId,
            GameId = request.GameId,
            TeamId = request.TeamId
        };

        dbContext.LobbyMemberPicks.Add(pick);
        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new PickMadeNotification(new(lobby, lobbyMember, pick)), ct);
        return mapper.Map<LobbyMemberPickDto>(pick);
    }
}