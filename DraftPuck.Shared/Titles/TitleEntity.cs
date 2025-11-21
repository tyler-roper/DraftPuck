using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Titles;

public partial class TitleEntity
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public Guid? AchievementId { get; set; }
    public AchievementEntity? Achievement { get; set; }
    public string Text { get; set; } = null!;

    public ICollection<UserTitleEntity> UserTitles { get; set; } = [];
}
