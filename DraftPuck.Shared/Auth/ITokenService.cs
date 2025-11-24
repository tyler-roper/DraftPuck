using DraftPuck.Shared.Users;

namespace DraftPuck.Shared.Auth;

public interface ITokenService
{
    string GenerateJwtToken(UserEntity user);
    UserRefreshTokenEntity GenerateRefreshToken(string ipAddress);
    string GenerateGuestJwtToken(Guid guestUserId);
}