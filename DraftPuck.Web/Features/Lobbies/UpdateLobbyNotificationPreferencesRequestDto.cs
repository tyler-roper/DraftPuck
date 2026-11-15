namespace DraftPuck.Web.Features.Lobbies;

public class UpdateLobbyNotificationPreferencesRequestDto
{
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
}
