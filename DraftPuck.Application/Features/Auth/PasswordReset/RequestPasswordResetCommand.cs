namespace DraftPuck.Application.Features.Auth.PasswordReset;

/// <summary>Emails a reset link to the account with this email, if there is one.</summary>
public class RequestPasswordResetCommand : IRequest
{
    public string Email { get; set; } = null!;
}
