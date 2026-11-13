namespace DraftPuck.Tests.Support;

public static class SeedAccounts
{
    public const string Password = "correct-horse-9";

    /// <summary>
    /// A registered (non-guest) user with <see cref="Password"/>.
    /// </summary>
    public static async Task<UserEntity> AccountAsync(DraftPuckContext db, string? nickname = null, bool isAdmin = false)
    {
        nickname ??= "user" + Guid.NewGuid().ToString("N")[..8];
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Nickname = nickname,
            Email = $"{nickname}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            IsAdmin = isAdmin,
            IsGuest = false
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
