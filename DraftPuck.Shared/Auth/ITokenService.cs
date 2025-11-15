namespace DraftPuck.Shared.Auth;

public interface ITokenService
{
    string GenerateJwtToken(Guid id);
    UserRefreshTokenEntity GenerateRefreshToken(string ipAddress);
    string GenerateGuestJwtToken(Guid guestUserId);
}