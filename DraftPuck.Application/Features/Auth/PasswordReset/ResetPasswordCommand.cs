namespace DraftPuck.Application.Features.Auth.PasswordReset;

/// <summary>Sets a new password using the token from a reset link.</summary>
public class ResetPasswordCommand : IRequest
{
    public string Token { get; set; } = null!;
    public string Password { get; set; } = null!;
}
