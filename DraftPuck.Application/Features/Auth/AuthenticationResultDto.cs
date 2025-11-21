using DraftPuck.Application.Features.Users;

namespace DraftPuck.Application.Features.Auth;

public class AuthenticationResultDto
{
    public UserDto User { get; set; } = null!;
    public string JwtToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public Guid AntiCsrfToken { get; set; }
}