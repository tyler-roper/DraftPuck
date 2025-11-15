namespace DraftPuck.Application.Common.QueryExtensions;

public static class UserQueryExtensions
{
    public static IQueryable<UserEntity> IncludeFullProfile(this IQueryable<UserEntity> query)
    {
        return query
            .Include(u => u.UserAchievements)
                .ThenInclude(ua => ua.Achievement)
            .Include(u => u.UserBanners)
                .ThenInclude(ub => ub.Banner)
            .Include(u => u.UserTitles)
                .ThenInclude(ut => ut.Title);
    }
}