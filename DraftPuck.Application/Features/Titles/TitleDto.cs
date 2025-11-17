namespace DraftPuck.Application.Features.Titles;

public class TitleDto
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public Guid? AchievementId { get; set; }
    public string Text { get; set; } = null!;
    public string? FriendlyName { get; set; }
    public string? Description { get; set; }
}
