namespace DraftPuck.Application.Features.Users;

/// <summary>Stops a device getting the user's push notifications, e.g. when they sign out on it.</summary>
public class ForgetDeviceCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}
