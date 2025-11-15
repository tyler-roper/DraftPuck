using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Shared.Auth;

namespace DraftPuck.Application.Features.Auth;

public class RevokeTokenCommandHandler(IDbContext dbContext, IUserRepository userRepository) : IRequestHandler<RevokeTokenCommand>
{
    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await GetUserByRefreshToken(request.Token!, cancellationToken);
        var refreshToken = user.RefreshTokens.Single(x => x.Token == request.Token!);

        if (!refreshToken.IsValid)
        {
            throw new BadRequestException("Token is invalid or already revoked.");
        }

        RevokeRefreshToken(refreshToken, request.IpAddress!, "Revoked without replacement");
        dbContext.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserEntity> GetUserByRefreshToken(string token, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByRefreshToken(token, cancellationToken);
        return user ?? throw new UnauthorizedException("Invalid token");
    }

    private static void RevokeRefreshToken(UserRefreshTokenEntity token, string ipAddress, string? reason = null, string? replacedByToken = null)
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
        token.ReplacedByToken = replacedByToken;
    }
}