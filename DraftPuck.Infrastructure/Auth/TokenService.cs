using DraftPuck.Shared.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DraftPuck.Infrastructure.Auth;

public class TokenService(AuthOptions options) : ITokenService
{
    public const string GuestClaimType = "guest";

    /// <summary>
    /// A short-lived access token for any user. Guests get a "guest" claim, so endpoints can tell them from signed-in
    /// users whichever way the token was issued (a new guest, or a returning guest's refresh). Guests renew through the
    /// refresh-token flow like everyone else.
    /// </summary>
    public string GenerateJwtToken(UserEntity user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(options.JwtKey!);
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (user.IsGuest)
            claims.Add(new Claim(GuestClaimType, "true"));

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public UserRefreshTokenEntity GenerateRefreshToken(string ipAddress)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = new UserRefreshTokenEntity
        {
            Token = Convert.ToBase64String(randomNumber),
            Expires = DateTime.UtcNow.AddDays(14),
            CreatedByIp = ipAddress,
            AntiCsrfToken = GenerateCryptographicallySecureGuid()
        };

        return refreshToken;
    }

    private static Guid GenerateCryptographicallySecureGuid()
    {
        byte[] bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }
}
