using DraftPuck.Application.Features.Banners;
using DraftPuck.Application.Features.Titles;
using DraftPuck.Domain.Friends;

namespace DraftPuck.Application.Features.Friends;

public static class FriendQueries
{
    /// <summary>The row for this pair, whichever of them asked.</summary>
    public static Task<FriendshipEntity?> BetweenAsync(this IQueryable<FriendshipEntity> friendships, Guid a, Guid b, CancellationToken ct) =>
        friendships.FirstOrDefaultAsync(f => (f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a), ct);

    /// <summary>The IDs of the user's accepted friends.</summary>
    public static IQueryable<Guid> FriendIdsOf(this IQueryable<FriendshipEntity> friendships, Guid userId) =>
        friendships
            .Where(f => f.Accepted != null && (f.RequesterId == userId || f.AddresseeId == userId))
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId);

    public static IQueryable<FriendDto> AsFriends(this IQueryable<UserEntity> users) =>
        users.Select(u => new FriendDto
        {
            Id = u.Id,
            Nickname = u.Nickname!,
            AvatarPath = u.AvatarPath,
            Banner = u.UserBanners.Where(ub => ub.IsEquipped).Select(ub => new BannerDto
            {
                Id = ub.Banner.Id,
                UniqueIdentifier = ub.Banner.UniqueIdentifier,
                AchievementId = ub.Banner.AchievementId,
                ImagePath = ub.Banner.ImagePath
            }).FirstOrDefault(),
            Title = u.UserTitles.Where(ut => ut.IsEquipped).Select(ut => new TitleDto
            {
                Id = ut.Title.Id,
                UniqueIdentifier = ut.Title.UniqueIdentifier,
                AchievementId = ut.Title.AchievementId,
                Text = ut.Title.Text
            }).FirstOrDefault()
        });
}
