using DraftPuck.Application.Common.QueryExtensions;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Admin.Lobbies;

public class GetAllLobbiesQueryHandler(IDbContext dbContext) : IRequestHandler<GetAllLobbiesQuery, IEnumerable<LobbySummaryDto>>
{
    public async Task<IEnumerable<LobbySummaryDto>> Handle(GetAllLobbiesQuery request, CancellationToken ct)
    {
        var pageSize = Math.Max(request.PageSize, 50);

        var lobbyQuery = dbContext.Lobbies
            .AsNoTracking()
            .IncludeLobbyDetails(request.IncludeRemovedUsers)
            .Where(l => l.IsActive || request.ActiveOnly);

        if (request.DateFrom != null)
            lobbyQuery = lobbyQuery.Where(l => l.Created >= request.DateFrom);

        if (request.DateTo != null)
            lobbyQuery = lobbyQuery.Where(l => l.Created <= request.DateTo);

        if (request.UserId != null)
            lobbyQuery = lobbyQuery.Where(l => l.CreatedBy == request.UserId || l.LobbyMembers.Any(lm => lm.UserId == request.UserId));

        var lobbies = await lobbyQuery
            .OrderByDescending(l => l.Created)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSummaries()
            .ToListAsync(ct);

        return lobbies;
    }
}