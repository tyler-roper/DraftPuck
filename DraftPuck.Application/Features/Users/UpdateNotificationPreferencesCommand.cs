namespace DraftPuck.Application.Features.Users;

/// <summary>
/// Sets the user's default notification settings: what each lobby they join starts with. With
/// <see cref="ApplyToActiveLobbies"/>, the lobby settings also change in every active lobby they're in.
/// </summary>
public class UpdateNotificationPreferencesCommand : IRequest<UserDto>
{
    public Guid UserId { get; set; }
    public NotificationPreference DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference ChatNotificationPreference { get; set; }
    public NotificationPreference PickingStartedNotificationPreference { get; set; }
    public NotificationPreference AchievementAwardedNotificationPreference { get; set; }
    public NotificationPreference RecapNotificationPreference { get; set; }
    public bool ApplyToActiveLobbies { get; set; }
}
