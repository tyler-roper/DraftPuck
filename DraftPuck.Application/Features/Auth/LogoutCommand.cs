namespace DraftPuck.Application.Features.Auth;

public class LogoutCommand : IRequest
{
    public Guid UserId { get; set; }
    /// <summary>The refresh token of the device signing out (its cookie). Only that device's session ends.</summary>
    public string? RefreshToken { get; set; }
    public string? IpAddress { get; set; }
}
