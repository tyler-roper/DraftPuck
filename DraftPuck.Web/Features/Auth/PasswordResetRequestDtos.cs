namespace DraftPuck.Web.Features.Auth;

public class RequestPasswordResetRequestDto
{
    public string Email { get; set; } = null!;
}

public class ResetPasswordRequestDto
{
    public string Token { get; set; } = null!;
    public string Password { get; set; } = null!;
}
