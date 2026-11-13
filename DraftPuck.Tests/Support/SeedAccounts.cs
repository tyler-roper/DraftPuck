namespace DraftPuck.Tests.Support;

public static class SeedAccounts
{
    public const string Password = "correct-horse-9";

    /// <summary>
    /// A registered (non-guest) user with <see cref="Password"/>. IsGuest is cleared after insert: EF omits a false bool on
    /// insert when the column has a database default, so the default (guest) would win.
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
            IsAdmin = isAdmin
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        user.IsGuest = false;
        await db.SaveChangesAsync();
        return user;
    }
}
