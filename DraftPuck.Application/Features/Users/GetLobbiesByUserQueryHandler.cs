using DraftPuck.Application.Common.QueryExtensions;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Users;

public class GetLobbiesByUserQueryHandler(IDbContext dbContext) : IRequestHandler<GetLobbiesByUserQuery, List<LobbySummaryDto>>
{
    public async Task<List<LobbySummaryDto>> Handle(GetLobbiesByUserQuery request, CancellationToken ct)
    {
        var lobbies = dbContext.Lobbies.Where(l => l.LobbyMembers.Any(m => m.UserId == request.UserId && !m.IsRemoved));
        if (request.ActiveOnly)
            lobbies = lobbies.Where(l => l.IsActive);

        return await lobbies
            .OrderByDescending(l => l.Created)
            .AsSummaries(request.UserId)
            .ToListAsync(ct);
    }
}
