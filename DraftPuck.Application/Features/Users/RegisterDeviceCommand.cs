namespace DraftPuck.Application.Features.Users;

/// <summary>Registers (or refreshes) the device a user is on, so it gets their push notifications.</summary>
public class RegisterDeviceCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    /// <summary>The browser's User-Agent header, to describe the device.</summary>
    public string? UserAgent { get; set; }
    /// <summary>Whether the app is running installed (home screen / desktop app) rather than in a browser tab.</summary>
    public bool IsInstalledApp { get; set; }
}
