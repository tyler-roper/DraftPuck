namespace DraftPuck.Application.Features.Achievements;

public class AchievementDto
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
