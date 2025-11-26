using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Events;

public class GetLobbyEventsQueryHandler(IDbContext dbContext, IMapper mapper) : IRequestHandler<GetLobbyEventsQuery, IEnumerable<LobbyEventDto>>
{
    public async Task<IEnumerable<LobbyEventDto>> Handle(GetLobbyEventsQuery request, CancellationToken ct)
    {
        var isMember = await dbContext.LobbyMembers
            .AnyAsync(lm => !lm.IsRemoved && lm.LobbyId == request.LobbyId && lm.UserId == request.UserId, ct);

        if (!isMember)
        {
            throw new UnauthorizedException("User not in lobby.");
        }

        var lobby = await dbContext.Lobbies.FindAsync([request.LobbyId], cancellationToken: ct) ?? throw new NotFoundException("Lobby not found.");
        var lobbyEvents = await dbContext.LobbyEvents
            .Where(e => e.LobbyId == request.LobbyId || e.LobbyId == null && e.Created >= lobby.Created && e.Created <= lobby.Created.AddHours(12))
            .OrderBy(e => e.TimeUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return mapper.Map<IEnumerable<LobbyEventDto>>(lobbyEvents);
    }
}