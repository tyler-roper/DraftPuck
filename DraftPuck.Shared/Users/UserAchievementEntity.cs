using DraftPuck.Shared.Achievements;

namespace DraftPuck.Shared.Users;

public class UserAchievementEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;

    public Guid AchievementId { get; set; }
    public AchievementEntity Achievement { get; set; } = null!;

    public DateTime DateEarned { get; set; } = DateTime.UtcNow;
}