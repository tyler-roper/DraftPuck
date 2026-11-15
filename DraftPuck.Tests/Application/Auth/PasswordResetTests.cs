using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Auth;
using DraftPuck.Application.Features.Auth.PasswordReset;
using DraftPuck.Shared.System;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DraftPuck.Tests.Application.Auth;

[Collection(DatabaseCollection.Name)]
public class PasswordResetTests(DatabaseFixture fixture)
{
    private const string NewPassword = "brand-new-pass-1";
    private readonly CapturingEmailSender _emails = new();

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private async Task RequestAsync(string email)
    {
        await using var db = fixture.CreateContext();
        var options = Options.Create(new ApplicationOptions { BasePath = "https://draftpuck.test" });
        await new RequestPasswordResetCommandHandler(db, _emails, options).Handle(new RequestPasswordResetCommand { Email = email }, default);
    }

    private async Task ResetAsync(string token, string password = NewPassword)
    {
        await using var db = fixture.CreateContext();
        await new ResetPasswordCommandHandler(db).Handle(new ResetPasswordCommand { Token = token, Password = password }, default);
    }

    private async Task<AuthenticationResultDto> LoginAsync(string email, string password)
    {
        await using var db = fixture.CreateContext();
        var handler = new LoginCommandHandler(db, TestServices.TokenService(), new UserRepository(db), TestServices.Mapper(), Substitute.For<IMediator>());
        return await handler.Handle(new LoginCommand { Email = email, Password = password, IpAddress = "203.0.113.7" }, default);
    }

    private async Task<UserEntity> AccountAsync()
    {
        await using var db = fixture.CreateContext();
        return await SeedAccounts.AccountAsync(db);
    }

    /// <summary>The token from the link in the last email sent.</summary>
    private string LastToken()
        => Regex.Match(_emails.Sent.Last().PlainText, @"reset-password\?token=([0-9A-F]+)").Groups[1].Value;

    [Fact]
    public async Task RequestEmailsALinkToTheAccount()
    {
        var account = await AccountAsync();

        await RequestAsync(account.Email!);

        var email = Assert.Single(_emails.Sent);
        Assert.Equal(account.Email, email.To);
        Assert.Contains("https://draftpuck.test/reset-password?token=", email.PlainText);
        Assert.Contains(account.Nickname!, email.Html);
        Assert.Equal(64, LastToken().Length);
    }

    [Fact]
    public async Task OnlyAHashOfTheTokenIsStored()
    {
        var account = await AccountAsync();

        await RequestAsync(account.Email!);

        await using var db = fixture.CreateContext();
        var stored = await db.PasswordResetTokens.SingleAsync(t => t.UserId == account.Id);
        Assert.NotEqual(LastToken(), stored.TokenHash);
        Assert.Equal(PasswordResetTokens.Hash(LastToken()), stored.TokenHash);
    }

    [Fact]
    public async Task RequestForAnUnknownEmailSendsNothingAndDoesNotFail()
    {
        await RequestAsync("nobody-" + Guid.NewGuid().ToString("N") + "@example.com");

        Assert.Empty(_emails.Sent);
    }

    [Fact]
    public async Task RequestIgnoresDeactivatedAccounts()
    {
        var account = await AccountAsync();
        await using (var db = fixture.CreateContext())
            await db.Users.Where(u => u.Id == account.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));

        await RequestAsync(account.Email!);

        Assert.Empty(_emails.Sent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RequestRequiresAnEmail(string email)
    {
        await Assert.ThrowsAsync<ValidationException>(() => RequestAsync(email));
    }

    [Fact]
    public async Task ResetChangesThePassword()
    {
        var account = await AccountAsync();
        await RequestAsync(account.Email!);

        await ResetAsync(LastToken());

        await LoginAsync(account.Email!, NewPassword);
        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(account.Email!, SeedAccounts.Password));
    }

    [Fact]
    public async Task ALinkWorksOnlyOnce()
    {
        var account = await AccountAsync();
        await RequestAsync(account.Email!);
        var token = LastToken();

        await ResetAsync(token);

        await Assert.ThrowsAsync<BadRequestException>(() => ResetAsync(token, "another-pass-22"));
    }

    [Fact]
    public async Task ANewRequestReplacesTheEarlierLink()
    {
        var account = await AccountAsync();
        await RequestAsync(account.Email!);
        var first = LastToken();
        await RequestAsync(account.Email!);
        var second = LastToken();

        await Assert.ThrowsAsync<BadRequestException>(() => ResetAsync(first));
        await ResetAsync(second);
    }

    [Fact]
    public async Task AnExpiredLinkIsRejected()
    {
        var account = await AccountAsync();
        await RequestAsync(account.Email!);
        await using (var db = fixture.CreateContext())
            await db.PasswordResetTokens.Where(t => t.UserId == account.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Expires, DateTime.UtcNow.AddMinutes(-1)));

        await Assert.ThrowsAsync<BadRequestException>(() => ResetAsync(LastToken()));
    }

    [Theory]
    [InlineData("not-a-real-token")]
    [InlineData("")]
    public async Task AnUnknownTokenIsRejected(string token)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => ResetAsync(token));
    }

    [Fact]
    public async Task AnInvalidPasswordIsRejectedWithoutUsingUpTheLink()
    {
        var account = await AccountAsync();
        await RequestAsync(account.Email!);

        await Assert.ThrowsAsync<ValidationException>(() => ResetAsync(LastToken(), "short"));

        await ResetAsync(LastToken());
    }

    [Fact]
    public async Task ResetSignsOutOtherSessions()
    {
        var account = await AccountAsync();
        var session = await LoginAsync(account.Email!, SeedAccounts.Password);
        await RequestAsync(account.Email!);

        await ResetAsync(LastToken());

        await using var db = fixture.CreateContext();
        var refresh = await db.UserRefreshTokens.SingleAsync(t => t.Token == session.RefreshToken);
        Assert.False(refresh.IsValid);
    }
}
