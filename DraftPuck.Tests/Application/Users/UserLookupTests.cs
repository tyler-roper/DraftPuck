using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Tests.Application.Users;

/// <summary>Looking players up: anyone's public profile by nickname, but only your own full account.</summary>
[Collection(DatabaseCollection.Name)]
public class UserLookupTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task AProfileByNicknameLeavesOutWhatIsPrivate()
    {
        await using var db = fixture.CreateContext();
        var account = await SeedAccounts.AccountAsync(db);
        account.DiscordUserId = "123456789";
        await db.SaveChangesAsync();

        var profile = await new GetUserByNameQueryHandler(TestServices.Mapper(), new UserRepository(db))
            .Handle(new GetUserByNameQuery { Name = account.Nickname! }, default);

        Assert.Equal(account.Id, profile.Id);
        Assert.Equal(account.Nickname, profile.Nickname);
        Assert.False(profile.IsGuest);
        // The profile type has no email, Discord ID or notification settings to leak.
        var fields = typeof(UserProfileDto).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(nameof(UserDto.Email), fields);
        Assert.DoesNotContain(nameof(UserDto.DiscordUserId), fields);
        Assert.DoesNotContain(fields, f => f.EndsWith("NotificationPreference"));
    }

    [Fact]
    public async Task OnlyYouCanLoadYourFullAccount()
    {
        await using var db = fixture.CreateContext();
        var me = await SeedAccounts.AccountAsync(db);
        var someoneElse = await SeedAccounts.AccountAsync(db);
        var handler = new GetUserByIdQueryHandler(TestServices.Mapper(), new UserRepository(db));

        Assert.Equal(me.Email, (await handler.Handle(new GetUserByIdQuery { Id = me.Id, RequesterId = me.Id }, default)).Email);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new GetUserByIdQuery { Id = someoneElse.Id, RequesterId = me.Id }, default));
    }
}
