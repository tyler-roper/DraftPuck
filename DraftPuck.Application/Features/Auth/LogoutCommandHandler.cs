namespace DraftPuck.Application.Features.Auth;

/// <summary>
/// Ends the session of the device signing out. Other devices stay signed in: signing out used to revoke every session,
/// which signed the user out on their phone whenever they signed out on their computer.
/// </summary>
public class LogoutCommandHandler(IDbContext dbContext) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return;

        var token = await dbContext.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == request.UserId, cancellationToken);
        if (token == null || !token.IsValid)
            return;

        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = request.IpAddress;
        token.ReasonRevoked = "Signed out";
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
