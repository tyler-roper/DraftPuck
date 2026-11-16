namespace DraftPuck.Domain.Users;

/// <summary>
/// A browser or installed app that receives a user's push notifications, by its Firebase registration token. A user
/// can have several (phone, computer); each gets every push.
/// </summary>
public class UserDeviceEntity
{
    /// <summary>Firebase tokens are a couple of hundred characters; this also keeps the unique index within SQL Server's limit.</summary>
    public const int MaxTokenLength = 450;
    public const int MaxDescriptionLength = 100;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public string Token { get; set; } = null!;
    /// <summary>What the device is, for people to read ("iPhone · installed app", "Windows · Chrome"). Informational only.</summary>
    public string? Description { get; set; }
    public DateTime Created { get; set; }
    /// <summary>When the device last registered: each time the app starts with notifications allowed.</summary>
    public DateTime LastSeenUtc { get; set; }
}
