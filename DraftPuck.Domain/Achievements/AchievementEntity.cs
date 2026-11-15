using DraftPuck.Domain.Banners;
using DraftPuck.Domain.Titles;
using DraftPuck.Domain.Users;

namespace DraftPuck.Domain.Achievements;

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
