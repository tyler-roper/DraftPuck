using DraftPuck.Application.Common.QueryExtensions;

namespace DraftPuck.Application.Features.Lobbies;

public class GetAllLobbiesQueryHandler(IDbContext dbContext, IMapper mapper) : IRequestHandler<GetAllLobbiesQuery, IEnumerable<LobbyDto>>
{
    public async Task<IEnumerable<LobbyDto>> Handle(GetAllLobbiesQuery request, CancellationToken ct)
    {
        var pageSize = Math.Max(request.PageSize, 50);

        var lobbyQuery = dbContext.Lobbies
            .AsNoTracking()
            .IncludeLobbyDetails(request.IncludeRemovedUsers)
            .Where(l => l.IsActive || request.ActiveOnly)
            .OrderByDescending(l => l.Created)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize);

        if (request.DateFrom != null)
            lobbyQuery = lobbyQuery.Where(l => l.Created >= request.DateFrom);

        if (request.DateTo != null)
            lobbyQuery = lobbyQuery.Where(l => l.Created <= request.DateTo);

        if (request.UserId != null)
            lobbyQuery = lobbyQuery.Where(l => l.CreatedBy == request.UserId || l.LobbyMembers.Any(lm => lm.UserId == request.UserId));

        var lobbies = await lobbyQuery.ToListAsync(ct);

        return mapper.Map<IEnumerable<LobbyDto>>(lobbies);
    }
}