namespace DraftPuck.Application.Features.Auth;

public class LogoutCommandHandler(IDbContext dbContext) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return;
        }

        user.RefreshTokens
            .Where(t => t.IsValid)
            .ToList()
            .ForEach(t => t.Revoked = DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}