using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Banners;
using DraftPuck.Application.Features.Titles;

namespace DraftPuck.Application.Features.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public bool IsGuest { get; set; }
    public bool IsBot { get; set; }
    public string? FcmRegistrationToken { get; set; }
    public BannerDto? Banner { get; set; }
    public TitleDto? Title { get; set; }
    public List<BannerDto> OwnedBanners { get; set; } = [];
    public List<TitleDto> OwnedTitles { get; set; } = [];
    public List<UserAchievementDto> Achievements { get; set; } = [];
    public string? DiscordUserId { get; set; }
    public DateTime? DiscordUserLinkedDate { get; set; }
    public string? AvatarPath { get; set; }
    public bool HasAvatar => !string.IsNullOrEmpty(AvatarPath);
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
    public NotificationPreference AchievementAwardedNotificationPreference { get; set; }
}
