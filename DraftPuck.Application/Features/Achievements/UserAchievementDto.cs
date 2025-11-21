namespace DraftPuck.Application.Features.Achievements;

public class UserAchievementDto
{
    public Guid AchievementId { get; set; }
    public string UniqueIdentifier { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DateEarned { get; set; }
}
