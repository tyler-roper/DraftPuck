using DraftPuck.Shared.Banners;
using DraftPuck.Shared.Titles;
using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Achievements;

public partial class AchievementEntity
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public string FriendlyName { get; set; } = null!;
    public string Description { get; set; } = null!;

    public ICollection<BannerEntity> Banners { get; set; } = [];
    public ICollection<TitleEntity> Titles { get; set; } = [];
    public ICollection<UserAchievementEntity> UserAchievements { get; set; } = [];
}
