namespace DraftPuck.Application.Features.Auth;

public class UseRefreshTokenCommand : IRequest<AuthenticationResultDto>
{
    public string RefreshToken { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string AntiCsrfTokenHeader { get; set; } = null!;
}