namespace DraftPuck.Application.Features.Banners;

public class BannerDto
{
    public Guid Id { get; set; }
    public string UniqueIdentifier { get; set; } = null!;
    public Guid? AchievementId { get; set; }
    public string ImagePath { get; set; } = null!;
}
