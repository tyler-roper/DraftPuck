using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Users;
using DraftPuck.Shared.Auth;

namespace DraftPuck.Application.Features.Auth;

public class UseRefreshTokenCommandHandler(IDbContext dbContext, ITokenService tokenService, IUserRepository userRepository, IMapper mapper) : IRequestHandler<UseRefreshTokenCommand, AuthenticationResultDto>
{
    public async Task<AuthenticationResultDto> Handle(UseRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userEntity = await GetUserByRefreshToken(request.RefreshToken, cancellationToken);
        var refreshTokenEntity = userEntity.RefreshTokens.Single(x => x.Token == request.RefreshToken);

        if (request.AntiCsrfTokenHeader == null || refreshTokenEntity.AntiCsrfToken.ToString() != request.AntiCsrfTokenHeader)
            throw new UnauthorizedException("Invalid CSRF token.");

        if (!refreshTokenEntity.IsValid)
        {
            RevokeDescendantRefreshTokens(refreshTokenEntity, userEntity, request.IpAddress!, $"Attempted reuse of revoked ancestor token: {request.RefreshToken}");
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Session terminated due to reuse of revoked or expired refresh token.");
        }

        var newRefreshToken = RotateRefreshToken(refreshTokenEntity, request.IpAddress!);
        userEntity.RefreshTokens.Add(newRefreshToken);
        RemoveOldRefreshTokens(userEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userWithDetails = await userRepository.GetById(userEntity.Id, cancellationToken);
        var userDto = mapper.Map<UserDto>(userWithDetails);

        var jwtToken = tokenService.GenerateJwtToken(userEntity);

        return new AuthenticationResultDto
        {
            User = userDto,
            JwtToken = jwtToken,
            RefreshToken = newRefreshToken.Token,
            AntiCsrfToken = newRefreshToken.AntiCsrfToken
        };
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