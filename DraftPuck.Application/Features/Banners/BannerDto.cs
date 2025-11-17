namespace DraftPuck.Application.Features.Banners;

public class BannerDto
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public Guid? AchievementId { get; set; }
    public string ImagePath { get; set; } = null!;
    public string? FriendlyName { get; set; }
    public string? Description { get; set; }
}
