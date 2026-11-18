namespace DraftPuck.Web.Features.Users;

public class UpdateNotificationPreferencesRequestDto
{
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
    public NotificationPreference AchievementAwardedNotificationPreference { get; set; }
    /// <summary>Optional, so older clients that don't send it leave it on.</summary>
    public NotificationPreference RecapNotificationPreference { get; set; } = NotificationPreference.All;
    public NotificationPreference FriendNotificationPreference { get; set; } = NotificationPreference.All;
    public bool ApplyToActiveLobbies { get; set; }
}
