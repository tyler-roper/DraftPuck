namespace DraftPuck.Application.Features.Auth;

public class LogoutCommand : IRequest
{
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
}