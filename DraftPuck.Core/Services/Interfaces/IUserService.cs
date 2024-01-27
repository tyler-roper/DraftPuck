namespace DraftPuck.Core.Services.Interfaces;

public interface IUserService
{
    public Task<User?> GetUserByIdAsync(Guid id);
    public Task<User> CreateUserAsync();
}
