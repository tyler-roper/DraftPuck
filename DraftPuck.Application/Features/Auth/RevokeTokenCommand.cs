namespace DraftPuck.Application.Features.Auth;

public class RevokeTokenCommand : IRequest
{
    public string Token { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
}