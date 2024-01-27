using DraftPuck.Data.Data;

namespace DraftPuck.Core.Services;

public class UserService : IUserService
{
    private readonly DraftPuckContext _dbContext;

    public UserService(DraftPuckContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _dbContext.Users.FindAsync(id);
    }

    public async Task<User> CreateUserAsync()
    {
        User user = new();
        _dbContext.Users.Add(new User());
        await _dbContext.SaveChangesAsync();
        return user;
    }
}
