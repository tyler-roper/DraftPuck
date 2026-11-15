using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Application.Features.Users;
using DraftPuck.Domain.Achievements;
using DraftPuck.Domain.Banners;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Tests.Application.Users;

[Collection(DatabaseCollection.Name)]
public class UserHandlerTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static string UniqueName() => "u" + Guid.NewGuid().ToString("N")[..10];

    private async Task<UserDto> RegisterAsync(Guid guestUserId, string? nickname = null, string? email = null, string password = "long-enough-1")
    {
        nickname ??= UniqueName();
        await using var db = fixture.CreateContext();
        return await new CreateUserCommandHandler(db, TestServices.Mapper(), _mediator).Handle(new CreateUserCommand
        {
            GuestUserId = guestUserId,
            Nickname = nickname,
            Email = email ?? $"{nickname}@example.com",
            Password = password
        }, default);
    }

    private async Task<UserDto> UpdateAsync(UpdateUserCommand command, IAvatarStorageService? avatars = null)
    {
        await using var db = fixture.CreateContext();
        return await new UpdateUserCommandHandler(db, TestServices.Mapper(), new UserRepository(db), _mediator,
                avatars ?? Substitute.For<IAvatarStorageService>(), NullLogger<UpdateUserCommandHandler>.Instance)
            .Handle(command, default);
    }

    // A 1x1 PNG, as the client sends it.
    private const string PngAvatarData =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    private async Task<UserEntity> AccountWithAvatarAsync(string avatarPath)
    {
        var account = await AccountAsync();
        await using var db = fixture.CreateContext();
        (await db.Users.SingleAsync(u => u.Id == account.Id)).AvatarPath = avatarPath;
        await db.SaveChangesAsync();
        return account;
    }

    private static UpdateUserCommand NewAvatar(UserEntity account) => new()
    {
        RequesterUserId = account.Id,
        TargetUserId = account.Id,
        RequesterIsAuthenticated = true,
        AvatarData = PngAvatarData
    };

    private async Task<UserEntity> GuestAsync()
    {
        await using var db = fixture.CreateContext();
        var guest = new Seed(db).User();
        await db.SaveChangesAsync();
        return guest;
    }

    private async Task<UserEntity> AccountAsync(bool isAdmin = false)
    {
        await using var db = fixture.CreateContext();
        return await SeedAccounts.AccountAsync(db, isAdmin: isAdmin);
    }

    // --- Registration ---

    [Fact]
    public async Task RegisteringWithoutAGuestAccountCreatesARegisteredUser()
    {
        var user = await RegisterAsync(Guid.NewGuid());

        await using var check = fixture.CreateContext();
        Assert.False((await check.Users.SingleAsync(u => u.Id == user.Id)).IsGuest);
    }

    [Fact]
    public async Task SigningUpWhileSignedInCannotOverwriteTheAccount()
    {
        // Converting a registered account would replace its email and password without the current password.
        var account = await AccountAsync();

        var created = await RegisterAsync(account.Id, email: "attacker@example.com", password: "attacker-pass");

        Assert.NotEqual(account.Id, created.Id);
        await using var check = fixture.CreateContext();
        var original = await check.Users.SingleAsync(u => u.Id == account.Id);
        Assert.Equal(account.Email, original.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify(SeedAccounts.Password, original.PasswordHash));
    }

    [Fact]
    public async Task RegisteringConvertsTheGuestInPlace()
    {
        var guest = await GuestAsync();

        var user = await RegisterAsync(guest.Id);

        Assert.Equal(guest.Id, user.Id);
        await using var check = fixture.CreateContext();
        var saved = await check.Users.Include(u => u.UserBanners).Include(u => u.UserTitles).SingleAsync(u => u.Id == guest.Id);
        Assert.False(saved.IsGuest);
        Assert.True(BCrypt.Net.BCrypt.Verify("long-enough-1", saved.PasswordHash));
        Assert.Single(saved.UserBanners, b => b.IsEquipped);
        Assert.Single(saved.UserTitles, t => t.IsEquipped);
    }

    [Fact]
    public async Task RegisteringRenamesTheGuestInItsLobbies()
    {
        var guest = await GuestAsync();
        Guid memberId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([Seed.NewGameId()]);
            memberId = seed.Member(lobby, await db.Users.SingleAsync(u => u.Id == guest.Id)).Id;
            await db.SaveChangesAsync();
        }

        var user = await RegisterAsync(guest.Id);

        await using var check = fixture.CreateContext();
        Assert.Equal(user.Nickname, (await check.LobbyMembers.SingleAsync(m => m.Id == memberId)).Name);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("       padded       ")] // under 8 characters once trimmed
    [InlineData("this-password-is-much-too-long")]
    public async Task RegisteringRejectsInvalidPasswords(string password)
    {
        await Assert.ThrowsAsync<ValidationException>(async () => await RegisterAsync((await GuestAsync()).Id, password: password));
    }

    [Fact]
    public async Task RegisteringRejectsTakenNicknamesAndEmails()
    {
        var existing = await AccountAsync();

        var takenName = await Assert.ThrowsAsync<ValidationException>(async () => await RegisterAsync((await GuestAsync()).Id, nickname: existing.Nickname));
        Assert.Contains("Nickname", takenName.Message);

        var takenEmail = await Assert.ThrowsAsync<ValidationException>(async () => await RegisterAsync((await GuestAsync()).Id, email: existing.Email));
        Assert.Contains("Email", takenEmail.Message);
    }

    // --- Updates ---

    [Fact]
    public async Task KeepingYourNicknameDoesNotLetYouTakeSomeoneElsesEmail()
    {
        // The settings page sends both fields. When the user's own nickname matched first, the other account's email
        // passed validation and only the unique index on Users.Email stopped it, as a 500 instead of this message.
        var victim = await AccountAsync();
        var account = await AccountAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = true,
            Nickname = account.Nickname,
            Email = victim.Email
        }));

        Assert.Equal("Email is already taken.", ex.Message);
    }

    [Fact]
    public async Task KeepingYourOwnNicknameAndEmailIsAllowed()
    {
        var account = await AccountAsync();

        await UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = true,
            Nickname = account.Nickname,
            Email = account.Email
        });
    }

    [Fact]
    public async Task UpdatedNicknamesAreTrimmedLikeNewOnes()
    {
        var account = await AccountAsync();
        var nickname = UniqueName();

        await UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = true,
            Nickname = $"  {nickname}  "
        });

        await using var check = fixture.CreateContext();
        Assert.Equal(nickname, (await check.Users.SingleAsync(u => u.Id == account.Id)).Nickname);
    }

    [Fact]
    public async Task RejectsANotificationTokenFarLongerThanARealOne()
    {
        var account = await AccountAsync();
        var tooLong = new string('a', UserValidationHelpers.MaxFcmRegistrationTokenLength + 1);

        await Assert.ThrowsAsync<ValidationException>(() => UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = true,
            FcmRegistrationToken = tooLong
        }));

        await using var db = fixture.CreateContext();
        await Assert.ThrowsAsync<ValidationException>(() => new UpdateFcmRegistrationTokenCommandHandler(db, new UserRepository(db), TestServices.Mapper())
            .Handle(new UpdateFcmRegistrationTokenCommand { UserId = account.Id, Token = tooLong }, default));
    }

    [Fact]
    public async Task ChangingYourPasswordRequiresTheCurrentOne()
    {
        var account = await AccountAsync();
        var change = new UpdateUserCommand { RequesterUserId = account.Id, TargetUserId = account.Id, RequesterIsAuthenticated = true, Password = "brand-new-pass" };

        await Assert.ThrowsAsync<BadRequestException>(() => UpdateAsync(change));
        change.CurrentPassword = "wrong-password";
        await Assert.ThrowsAsync<BadRequestException>(() => UpdateAsync(change));

        change.CurrentPassword = SeedAccounts.Password;
        await UpdateAsync(change);

        await using var check = fixture.CreateContext();
        Assert.True(BCrypt.Net.BCrypt.Verify("brand-new-pass", (await check.Users.SingleAsync(u => u.Id == account.Id)).PasswordHash));
    }

    [Fact]
    public async Task OtherSettingsDoNotNeedThePassword()
    {
        var account = await AccountAsync();

        await UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = true,
            FcmRegistrationToken = "new-device-token"
        });

        await using var check = fixture.CreateContext();
        Assert.Equal("new-device-token", (await check.Users.SingleAsync(u => u.Id == account.Id)).FcmRegistrationToken);
    }

    [Fact]
    public async Task UsersCannotUpdateSomeoneElse()
    {
        var account = await AccountAsync();
        var victim = await AccountAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = victim.Id,
            RequesterIsAuthenticated = true,
            Nickname = UniqueName()
        }));
    }

    [Fact]
    public async Task AdminsCanResetSomeoneElsesPasswordWithoutTheirCurrentOne()
    {
        var admin = await AccountAsync(isAdmin: true);
        var user = await AccountAsync();

        await UpdateAsync(new UpdateUserCommand { RequesterUserId = admin.Id, TargetUserId = user.Id, RequesterIsAuthenticated = true, Password = "reset-by-admin" });

        await using var check = fixture.CreateContext();
        Assert.True(BCrypt.Net.BCrypt.Verify("reset-by-admin", (await check.Users.SingleAsync(u => u.Id == user.Id)).PasswordHash));
    }

    [Fact]
    public async Task AGuestTokenCannotActAsARegisteredAccount()
    {
        var account = await AccountAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => UpdateAsync(new UpdateUserCommand
        {
            RequesterUserId = account.Id,
            TargetUserId = account.Id,
            RequesterIsAuthenticated = false,
            Nickname = UniqueName()
        }));
    }

    [Fact]
    public async Task EquippingABannerRequiresItsAchievement()
    {
        var account = await AccountAsync();
        Guid lockedBannerId, genericBannerId, achievementId;
        await using (var db = fixture.CreateContext())
        {
            var achievement = new AchievementEntity { Id = Guid.NewGuid(), UniqueIdentifier = UniqueName(), FriendlyName = "Test", Description = "Test" };
            var locked = new BannerEntity { Id = Guid.NewGuid(), UniqueIdentifier = UniqueName(), ImagePath = "/locked.png", AchievementId = achievement.Id };
            var generic = new BannerEntity { Id = Guid.NewGuid(), UniqueIdentifier = UniqueName(), ImagePath = "/generic.png" };
            db.AddRange(achievement, locked, generic);
            await db.SaveChangesAsync();
            (lockedBannerId, genericBannerId, achievementId) = (locked.Id, generic.Id, achievement.Id);
        }

        UpdateUserCommand Equip(Guid bannerId) => new() { RequesterUserId = account.Id, TargetUserId = account.Id, RequesterIsAuthenticated = true, BannerId = bannerId };

        await Assert.ThrowsAsync<ForbiddenException>(() => UpdateAsync(Equip(lockedBannerId)));
        await UpdateAsync(Equip(genericBannerId));

        await using (var db = fixture.CreateContext())
        {
            db.UserAchievements.Add(new UserAchievementEntity { Id = Guid.NewGuid(), UserId = account.Id, AchievementId = achievementId });
            await db.SaveChangesAsync();
        }
        await UpdateAsync(Equip(lockedBannerId));

        await using var check = fixture.CreateContext();
        var equipped = await check.UserBanners.Where(b => b.UserId == account.Id && b.IsEquipped).ToListAsync();
        Assert.Equal(lockedBannerId, Assert.Single(equipped).BannerId);
    }

    // --- Avatars ---

    [Fact]
    public async Task AFailedAvatarUploadKeepsTheCurrentAvatar()
    {
        var account = await AccountWithAvatarAsync("https://storage.example/avatars/old.png");
        var avatars = Substitute.For<IAvatarStorageService>();
        avatars.UploadBlobAsync(default!, default!, default!).ReturnsForAnyArgs<string>(_ => throw new IOException("storage down"));

        await UpdateAsync(NewAvatar(account), avatars);

        await using var check = fixture.CreateContext();
        Assert.Equal("https://storage.example/avatars/old.png", (await check.Users.SingleAsync(u => u.Id == account.Id)).AvatarPath);
        await avatars.DidNotReceiveWithAnyArgs().DeleteBlobByUriAsync(default!);
    }

    [Fact]
    public async Task FailingToDeleteTheOldAvatarStillSavesTheNewOne()
    {
        var account = await AccountWithAvatarAsync("https://storage.example/avatars/old.png");
        var avatars = Substitute.For<IAvatarStorageService>();
        avatars.UploadBlobAsync(default!, default!, default!).ReturnsForAnyArgs("https://storage.example/avatars/new.png");
        avatars.DeleteBlobByUriAsync(default!).ReturnsForAnyArgs(_ => throw new IOException("storage down"));

        await UpdateAsync(NewAvatar(account), avatars);

        await using var check = fixture.CreateContext();
        Assert.Equal("https://storage.example/avatars/new.png", (await check.Users.SingleAsync(u => u.Id == account.Id)).AvatarPath);
        await avatars.Received().DeleteBlobByUriAsync("https://storage.example/avatars/old.png");
    }
}
