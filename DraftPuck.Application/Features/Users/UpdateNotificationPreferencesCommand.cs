namespace DraftPuck.Application.Features.Users;

public class UpdateNotificationPreferencesCommand : IRequest<UserDto>
{
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
    public NotificationPreference AchievementAwardedNotificationPreference { get; set; }
    public Guid UserId { get; set; }
}
