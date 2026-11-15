using System.Security.Cryptography;
using System.Text;

namespace DraftPuck.Application.Features.Auth.PasswordReset;

/// <summary>Creating and hashing "forgot password" tokens.</summary>
public static class PasswordResetTokens
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    /// <summary>A random token for the link: 32 bytes as hex, so it's URL-safe as is.</summary>
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>What's stored: the token's SHA-256. A random token needs no salt or slow hash.</summary>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
