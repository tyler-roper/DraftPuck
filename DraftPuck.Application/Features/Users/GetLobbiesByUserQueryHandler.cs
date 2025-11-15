using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Users;
public class GetLobbiesByUserQueryHandler(IDbContext dbContext) : IRequestHandler<GetLobbiesByUserQuery, List<UserLobbySummaryDto>>
{
    public async Task<List<UserLobbySummaryDto>> Handle(GetLobbiesByUserQuery request, CancellationToken ct)
    {
        var lobbyEntities = await dbContext.Lobbies
            .Where(l => l.LobbyMembers.Any(m => m.UserId == request.UserId && !m.IsRemoved))
            .Select(l => new UserLobbySummaryDto()
            {
                Id = l.Id,
                IsActive = l.IsActive,
                JoinCode = l.JoinCode,
                Created = l.Created,
                CreatedBy = l.CreatedBy,
                PicksPerTeam = l.PicksPerTeam,
                IsBotAutoPickingEnabled = l.IsBotAutoPickingEnabled,
                GameCount = l.GameIds.Count,
                MemberCount = l.LobbyMembers.Count,
                DrinksGiven = l.LobbyMembers
                    .Where(lm => lm.UserId == request.UserId)
                    .SelectMany(lm => lm.LobbyMemberPicks)
                    .SelectMany(lmp => lmp.Drinks)
                    .Count(d => d.RecipientLobbyMemberId != null),
                DrinksTaken = l.LobbyMembers
                    .SelectMany(lm => lm.LobbyMemberPicks)
                    .SelectMany(lmp => lmp.Drinks)
                    .Count(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == request.UserId),
                DrinksPending = l.LobbyMembers
                    .Where(lm => lm.UserId == request.UserId)
                    .SelectMany(lm => lm.LobbyMemberPicks)
                    .SelectMany(lmp => lmp.Drinks)
                    .Count(d => d.RecipientLobbyMemberId == null)
            })
            .ToListAsync(ct);

        return lobbyEntities;
    }
}
