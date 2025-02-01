using DraftPuck.Infrastructure.Database;

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
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateFcmRegistrationTokenAsync(Guid id, UpdateFcmRegistrationTokenRequestModel model)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return null;

        user.FcmRegistrationToken = model.Token;
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateNotificationPreferencesAsync(Guid id, UserNotificationPreferencesRequestModel model)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return null;

        user.DrinkReceivedNotificationPreference = model.DrinkReceivedNotificationPreference;
        user.DrinkAwardedNotificationPreference = model.DrinkAwardedNotificationPreference;
        user.ChatNotificationPreference = model.ChatNotificationPreference;
        await _dbContext.SaveChangesAsync();
        return user;
    }
}
