using DraftPuck.Shared.Auth;
using DraftPuck.Shared.Lobbies;
using System.Text.Json.Serialization;

namespace DraftPuck.Shared.Users;

public partial class UserEntity
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public bool IsGuest { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsBot { get; set; } = false;
    public string? AvatarPath { get; set; }
    public DateTime Created { get; set; }
    public string? FcmRegistrationToken { get; set; }
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference ChatNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference PickingStartedNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference AchievementAwardedNotificationPreference { get; set; } = NotificationPreference.None;
    [JsonIgnore]
    public string? PasswordHash { get; set; }
    public string? DiscordUserId { get; set; }
    public DateTime? DiscordUserLinkedDate { get; set; }

    public ICollection<UserBannerEntity> UserBanners { get; set; } = [];
    public ICollection<UserTitleEntity> UserTitles { get; set; } = [];
    public ICollection<UserAchievementEntity> UserAchievements { get; set; } = [];
    public ICollection<LobbyMemberEntity> LobbyMembers { get; } = [];
    public ICollection<LobbyEntity> CreatedLobbies { get; } = [];
    public ICollection<UserRefreshTokenEntity> RefreshTokens { get; set; } = [];
}
