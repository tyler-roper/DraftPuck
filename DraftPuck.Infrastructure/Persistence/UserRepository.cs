using DraftPuck.Application.Common.QueryExtensions;

namespace DraftPuck.Infrastructure.Persistence;

public class UserRepository(DraftPuckContext dbContext) : IUserRepository
{
    public async Task<UserEntity?> GetById(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .IncludeFullProfile()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<UserEntity?> GetByName(string name, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .IncludeFullProfile()
            .FirstOrDefaultAsync(u => u.Nickname == name, cancellationToken);
    }

    public async Task<UserEntity?> GetByRefreshToken(string token, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token), cancellationToken);
    }
}