using DraftPuck.Shared.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace DraftPuck.Shared.Auth;

public partial class UserRefreshTokenEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Token { get; set; } = null!;
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = null!;
    public DateTime? Revoked { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReasonRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public Guid AntiCsrfToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsValid => !IsRevoked && !IsExpired;

    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;
}
