using System.Linq.Expressions;

namespace DraftPuck.Application.Common.QueryExtensions;
public static class LobbyQueryExtensions
{
    public static IQueryable<LobbyEntity> IncludeLobbyDetails(this IQueryable<LobbyEntity> query, bool includeRemoved = false)
    {
        Expression<Func<LobbyEntity, IEnumerable<LobbyMemberEntity>>> lobbyMembersSelector =
                l => l.LobbyMembers.Where(lm => includeRemoved || !lm.IsRemoved);

        return query
            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.LobbyMemberPicks
                    .Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)

            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.Messages
                    .Where(m => includeRemoved || !m.IsDeleted))

            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.User)
                    .ThenInclude(u => u.UserBanners)
                        .ThenInclude(ub => ub.Banner)

            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.User)
                    .ThenInclude(u => u.UserTitles)
                        .ThenInclude(ut => ut.Title);
    }
}