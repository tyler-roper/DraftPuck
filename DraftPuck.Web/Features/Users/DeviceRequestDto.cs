namespace DraftPuck.Web.Features.Users;

public class DeviceRequestDto
{
    /// <summary>The device's Firebase registration token.</summary>
    public string Token { get; set; } = null!;
    /// <summary>Whether the app is running installed (home screen / desktop app), which the User-Agent doesn't say.</summary>
    public bool IsInstalledApp { get; set; }
}
