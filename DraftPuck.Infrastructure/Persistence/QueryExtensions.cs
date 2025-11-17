namespace DraftPuck.Infrastructure.Persistence;
public static class LobbyQueryExtensions
{
    public static IQueryable<LobbyEntity> IncludeLobbyDetails(this IQueryable<LobbyEntity> query)
    {
        return query
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted));
    }
}