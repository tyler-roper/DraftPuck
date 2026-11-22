using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Users;
using DraftPuck.Domain.Auth;

namespace DraftPuck.Application.Features.Auth;

public class UseRefreshTokenCommandHandler(IDbContext dbContext, ITokenService tokenService, IUserRepository userRepository, IMapper mapper) : IRequestHandler<UseRefreshTokenCommand, AuthenticationResultDto>
{
    /// <summary>
    /// A token that comes back this soon after being replaced is the same app asking again, not theft: two refreshes at
    /// once (on start, a 401, the live connection, another tab), or a retry after the first answer never arrived (the app
    /// reloaded or was closed mid-request). It gets the session it was replaced by, so the app stays signed in; nothing
    /// else would, as the replacement token may have reached nobody. Later reuse is treated as theft.
    /// </summary>
    public static readonly TimeSpan ConcurrentRefreshGrace = TimeSpan.FromSeconds(30);

    public async Task<AuthenticationResultDto> Handle(UseRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userEntity = await GetUserByRefreshToken(request.RefreshToken, cancellationToken);
        var refreshTokenEntity = userEntity.RefreshTokens.Single(x => x.Token == request.RefreshToken);

        if (request.AntiCsrfTokenHeader == null || refreshTokenEntity.AntiCsrfToken.ToString() != request.AntiCsrfTokenHeader)
            throw new UnauthorizedException("Invalid CSRF token.");

        UserRefreshTokenEntity sessionToken;
        if (refreshTokenEntity.IsValid)
        {
            sessionToken = RotateRefreshToken(refreshTokenEntity, request.IpAddress!);
            userEntity.RefreshTokens.Add(sessionToken);
            RemoveOldRefreshTokens(userEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (WasJustReplaced(refreshTokenEntity))
        {
            sessionToken = CurrentReplacement(refreshTokenEntity, userEntity)
                ?? throw new UnauthorizedException("Refresh token was just replaced, and that session has since ended.");
        }
        else
        {
            RevokeDescendantRefreshTokens(refreshTokenEntity, userEntity, request.IpAddress!, $"Attempted reuse of revoked ancestor token: {request.RefreshToken}");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Session terminated due to reuse of revoked or expired refresh token.");
        }

        var userWithDetails = await userRepository.GetById(userEntity.Id, cancellationToken);
        var userDto = mapper.Map<UserDto>(userWithDetails);

        var jwtToken = tokenService.GenerateJwtToken(userEntity);

        return new AuthenticationResultDto
        {
            User = userDto,
            JwtToken = jwtToken,
            RefreshToken = sessionToken.Token,
            AntiCsrfToken = sessionToken.AntiCsrfToken
        };
    }

    private static bool WasJustReplaced(UserRefreshTokenEntity token) =>
        token.ReplacedByToken != null && token.Revoked > DateTime.UtcNow - ConcurrentRefreshGrace;

    /// <summary>
    /// The token that now carries a replaced token's session: its replacement, or that one's if it has been replaced
    /// too. Null if the session has ended (signed out, revoked or expired).
    /// </summary>
    private static UserRefreshTokenEntity? CurrentReplacement(UserRefreshTokenEntity token, UserEntity user)
    {
        var current = token;
        while (current.ReplacedByToken != null)
        {
            var next = user.RefreshTokens.SingleOrDefault(t => t.Token == current.ReplacedByToken);
            if (next == null) return null;
            current = next;
        }

        return current == token || !current.IsValid ? null : current;
    }

    private async Task<UserEntity> GetUserByRefreshToken(string token, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshToken(token, cancellationToken);

        return user ?? throw new UnauthorizedException("Invalid token");
    }

    private static void RevokeDescendantRefreshTokens(UserRefreshTokenEntity refreshToken, UserEntity user, string ipAddress, string reason)
    {
        if (string.IsNullOrEmpty(refreshToken.ReplacedByToken))
        {
            return;
        }

        var childToken = user.RefreshTokens
            .SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);

        if (childToken == null)
        {
            return;
        }

        if (childToken.IsValid)
        {
            RevokeRefreshToken(childToken, ipAddress, reason);
        }
        else
        {
            RevokeDescendantRefreshTokens(childToken, user, ipAddress, reason);
        }
    }

    private static void RevokeRefreshToken(UserRefreshTokenEntity token, string ipAddress, string? reason = null, string? replacedByToken = null)
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
        token.ReplacedByToken = replacedByToken;
    }

    private UserRefreshTokenEntity RotateRefreshToken(UserRefreshTokenEntity refreshToken, string ipAddress)
    {
        var newRefreshToken = tokenService.GenerateRefreshToken(ipAddress);
        RevokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
        return newRefreshToken;
    }

    private void RemoveOldRefreshTokens(UserEntity userEntity)
    {
        dbContext.UserRefreshTokens.RemoveRange(userEntity.RefreshTokens.Where(t => !t.IsValid && t.Created.AddDays(2) <= DateTime.UtcNow));
    }
}