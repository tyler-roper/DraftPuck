using System.ComponentModel.DataAnnotations;
using DraftPuck.Application.Features.Users;
using DraftPuck.Infrastructure.Firebase;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Users;

/// <summary>Registering and forgetting the devices that get a user's push notifications.</summary>
[Collection(DatabaseCollection.Name)]
public class DeviceTests(DatabaseFixture fixture)
{
    private const string IPhoneSafari = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1";
    private const string WindowsChrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

    private static string NewToken() => "tok-" + Guid.NewGuid().ToString("N");

    private async Task<UserEntity> AccountAsync()
    {
        await using var db = fixture.CreateContext();
        return await SeedAccounts.AccountAsync(db);
    }

    private async Task RegisterAsync(Guid userId, string token, string? userAgent = null, bool installed = false)
    {
        await using var db = fixture.CreateContext();
        await new RegisterDeviceCommandHandler(db, Options.Create(new ApplicationOptions()))
            .Handle(new RegisterDeviceCommand { UserId = userId, Token = token, UserAgent = userAgent, IsInstalledApp = installed }, default);
    }

    private async Task ForgetAsync(Guid userId, string token)
    {
        await using var db = fixture.CreateContext();
        await new ForgetDeviceCommandHandler(db).Handle(new ForgetDeviceCommand { UserId = userId, Token = token }, default);
    }

    private async Task<List<UserDeviceEntity>> DevicesAsync(Guid userId)
    {
        await using var db = fixture.CreateContext();
        return await db.UserDevices.Where(d => d.UserId == userId).ToListAsync();
    }

    [Fact]
    public async Task EachDeviceTheUserRegistersIsKept()
    {
        // The bug: one token per user, so the phone stopped getting pushes once the computer registered.
        var user = await AccountAsync();
        var (phone, computer) = (NewToken(), NewToken());

        await RegisterAsync(user.Id, phone, IPhoneSafari, installed: true);
        await RegisterAsync(user.Id, computer, WindowsChrome);

        var devices = await DevicesAsync(user.Id);
        Assert.Equal(new[] { phone, computer }.Order(), devices.Select(d => d.Token).Order());
        Assert.Equal("iPhone · installed app", devices.Single(d => d.Token == phone).Description);
        Assert.Equal("Windows · Chrome", devices.Single(d => d.Token == computer).Description);
    }

    [Fact]
    public async Task RegisteringAgainRefreshesTheDeviceRatherThanAddingIt()
    {
        var user = await AccountAsync();
        var token = NewToken();

        await RegisterAsync(user.Id, token);
        var firstSeen = (await DevicesAsync(user.Id)).Single().LastSeenUtc;
        await RegisterAsync(user.Id, $"  {token}  ");

        var device = Assert.Single(await DevicesAsync(user.Id));
        Assert.True(device.LastSeenUtc >= firstSeen);
    }

    [Fact]
    public async Task ADeviceSomeoneElseSignsInOnMovesToThem()
    {
        var before = await AccountAsync();
        var after = await AccountAsync();
        var token = NewToken();

        await RegisterAsync(before.Id, token);
        await RegisterAsync(after.Id, token);

        Assert.Empty(await DevicesAsync(before.Id));
        Assert.Single(await DevicesAsync(after.Id));
    }

    [Fact]
    public async Task ForgettingRemovesOnlyThatDeviceAndOnlyYourOwn()
    {
        var user = await AccountAsync();
        var other = await AccountAsync();
        var (phone, computer, othersPhone) = (NewToken(), NewToken(), NewToken());
        await RegisterAsync(user.Id, phone);
        await RegisterAsync(user.Id, computer);
        await RegisterAsync(other.Id, othersPhone);

        await ForgetAsync(user.Id, computer);
        await ForgetAsync(user.Id, othersPhone);

        Assert.Equal([phone], (await DevicesAsync(user.Id)).Select(d => d.Token));
        Assert.Single(await DevicesAsync(other.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has a space")]
    public async Task RejectsTokensThatCouldNotBeReal(string token)
    {
        var user = await AccountAsync();

        await Assert.ThrowsAsync<ValidationException>(() => RegisterAsync(user.Id, token));
    }

    [Fact]
    public async Task RejectsATokenLongerThanARealOne()
    {
        var user = await AccountAsync();

        await Assert.ThrowsAsync<ValidationException>(() => RegisterAsync(user.Id, new string('a', UserDeviceEntity.MaxTokenLength + 1)));
    }

    [Theory]
    [InlineData(IPhoneSafari, false, "iPhone · Safari")]
    [InlineData(WindowsChrome, false, "Windows · Chrome")]
    [InlineData("Mozilla/5.0 (Linux; Android 15; Pixel 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Mobile Safari/537.36", true, "Android · installed app")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0", false, "Windows · Edge")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:144.0) Gecko/20100101 Firefox/144.0", false, "Mac · Firefox")]
    [InlineData(null, false, null)]
    public void DescribesTheDevice(string? userAgent, bool installed, string? expected)
    {
        Assert.Equal(expected, DeviceDescriptions.Describe(userAgent, installed));
    }

    [Theory]
    [InlineData(MessagingErrorCode.Unregistered, true)]
    [InlineData(MessagingErrorCode.SenderIdMismatch, true)]
    [InlineData(MessagingErrorCode.Unavailable, false)]
    [InlineData(MessagingErrorCode.QuotaExceeded, false)]
    [InlineData(MessagingErrorCode.Internal, false)]
    public void OnlyPermanentFirebaseErrorsRemoveADevice(MessagingErrorCode code, bool dead)
    {
        Assert.Equal(dead, FirebaseService.IsDeadToken(code));
    }
}
