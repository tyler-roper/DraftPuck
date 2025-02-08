namespace DraftPuck.Shared.Entities;

public class UserNotificationPreferencesRequestModel
{
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference ChatNotificationPreference { get; set; } = NotificationPreference.None;
    public NotificationPreference PickingStartedNotificationPreference { get; set; } = NotificationPreference.None;
}
