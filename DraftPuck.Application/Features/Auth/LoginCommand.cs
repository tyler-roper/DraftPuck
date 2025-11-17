namespace DraftPuck.Application.Features.Auth;

public class LoginCommand : IRequest<AuthenticationResultDto>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public Guid GuestUserId { get; set; }
}