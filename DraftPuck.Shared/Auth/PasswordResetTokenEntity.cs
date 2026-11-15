using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Auth;

/// <summary>
/// A pending "forgot password" link. Only a hash of the token is stored; the token itself exists only in the email.
/// A user has at most one: requesting a new link replaces the old one, and using a link deletes it.
/// </summary>
public class PasswordResetTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public UserEntity User { get; set; } = null!;
}
