namespace DraftPuck.Application.Features.Users;
public class UpdateUserCommand : IRequest<UserDto>
{
    public Guid TargetUserId { get; set; }
    public Guid RequesterUserId { get; set; }
    public bool RequesterIsAuthenticated { get; set; }
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public string? FcmRegistrationToken { get; set; }
    public string? Password { get; set; }
    public string? AvatarData { get; set; }
    public NotificationPreference? DrinkReceivedNotificationPreference { get; set; }
    public NotificationPreference? DrinkAwardedNotificationPreference { get; set; }
    public NotificationPreference? ChatNotificationPreference { get; set; }
    public NotificationPreference? PickingStartedNotificationPreference { get; set; }
    public NotificationPreference? AchievementAwardedNotificationPreference { get; set; }
    public Guid? BannerId { get; set; }
    public Guid? TitleId { get; set; }
}