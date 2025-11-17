using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Banners;

public class BannerEntity
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public Guid? AchievementId { get; set; }
    public AchievementEntity? Achievement { get; set; }
    public string ImagePath { get; set; } = null!;

    public ICollection<UserBannerEntity> UserBanners { get; set; } = [];
}