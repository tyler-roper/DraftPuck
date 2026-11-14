using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Auth;
using DraftPuck.Application.Features.Lobbies.Management;
using DraftPuck.Application.Features.Lobbies.Messages;
using DraftPuck.Application.Features.Users;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Web.Filters;

namespace DraftPuck.Tests.Application;

/// <summary>Rules the API must enforce itself, whatever the client does.</summary>
[Collection(DatabaseCollection.Name)]
public class HardeningTests(DatabaseFixture fixture)
{
    /// <summary>The token's claims, decoded directly so the test doesn't depend on a JWT library's reader.</summary>
    private static JsonElement Payload(string jwt)
    {
        var base64 = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return JsonDocument.Parse(Convert.FromBase64String(base64)).RootElement;
    }

    private static bool HasGuestClaim(string jwt)
        => Payload(jwt).TryGetProperty(TokenService.GuestClaimType, out var guest) && guest.GetString() == "true";

    // --- Guest tokens ---

    [Fact]
    public async Task GuestsStayGuestsWhenTheirTokenIsRefreshed()
    {
        // The site refreshes on every visit, so a returning guest's token comes from the refresh endpoint.
        AuthenticationResultDto created;
        await using (var db = fixture.CreateContext())
        {
            created = await new CreateGuestCommandHandler(db, TestServices.Mapper(), TestServices.TokenService(), new UserRepository(db))
                .Handle(new CreateGuestCommand { IpAddress = "127.0.0.1" }, default);
        }

        AuthenticationResultDto refreshed;
        await using (var db = fixture.CreateContext())
        {
            refreshed = await new UseRefreshTokenCommandHandler(db, TestServices.TokenService(), new UserRepository(db), TestServices.Mapper())
                .Handle(new UseRefreshTokenCommand { RefreshToken = created.RefreshToken, AntiCsrfTokenHeader = created.AntiCsrfToken.ToString(), IpAddress = "127.0.0.1" }, default);
        }

        Assert.True(HasGuestClaim(created.JwtToken));
        Assert.True(HasGuestClaim(refreshed.JwtToken));
    }

    [Fact]
    public async Task AccountsDoNotGetTheGuestClaim()
    {
        await using var db = fixture.CreateContext();
        var account = await SeedAccounts.AccountAsync(db);

        Assert.False(HasGuestClaim(TestServices.TokenService().GenerateJwtToken(account)));
    }

    [Fact]
    public void AccessTokensAreShortLivedForGuestsToo()
    {
        var jwt = TestServices.TokenService().GenerateJwtToken(new UserEntity { Id = Guid.NewGuid(), IsGuest = true });

        var lifetime = DateTimeOffset.FromUnixTimeSeconds(Payload(jwt).GetProperty("exp").GetInt64()) - DateTimeOffset.UtcNow;
        Assert.InRange(lifetime, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(16));
    }

    // --- Internal API key ---

    [Theory]
    [InlineData("secret", "secret", true)]
    [InlineData("wrong", "secret", false)]
    [InlineData("secre", "secret", false)]
    [InlineData("", "secret", false)]
    [InlineData(null, "secret", false)]
    [InlineData("", "", false)]      // an unset key must not match an empty header
    [InlineData(null, null, false)]
    [InlineData("anything", null, false)]
    public void InternalApiKeyMustBeConfiguredAndMatch(string? provided, string? configured, bool expected)
        => Assert.Equal(expected, InternalApiAuthFilter.IsAuthorized(provided, configured));

    // --- Chat messages ---

    private async Task<(string Code, Guid HostUserId)> LobbyAsync()
    {
        await using var db = fixture.CreateContext();
        var (lobby, host) = new Seed(db).Lobby([Seed.NewGameId()]);
        await db.SaveChangesAsync();
        return (lobby.JoinCode, host.UserId);
    }

    private async Task SendAsync(string code, Guid userId, string message)
    {
        await using var db = fixture.CreateContext();
        await new SendLobbyMessageCommandHandler(db, Substitute.For<IMediator>())
            .Handle(new SendLobbyMessageCommand { Code = code, UserId = userId, Message = message }, default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyMessagesAreRejected(string message)
    {
        var (code, userId) = await LobbyAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => SendAsync(code, userId, message));
    }

    [Fact]
    public async Task MessagesOverTheLimitAreRejectedAndOthersAreTrimmed()
    {
        var (code, userId) = await LobbyAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => SendAsync(code, userId, new string('a', SendLobbyMessageCommandHandler.MaxMessageLength + 1)));
        await SendAsync(code, userId, "  " + new string('a', SendLobbyMessageCommandHandler.MaxMessageLength) + "  ");

        await using var db = fixture.CreateContext();
        var saved = Assert.Single(await db.Messages.Where(m => m.LobbyMember.Lobby.JoinCode == code).ToListAsync());
        Assert.Equal(SendLobbyMessageCommandHandler.MaxMessageLength, saved.Message.Length);
    }

    // --- Lobby settings ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    public void PicksPerTeamMustBeInRange(int picksPerTeam)
        => Assert.Throws<ValidationException>(() => LobbyRules.ValidateNewLobby(picksPerTeam, [1], botCount: 0));

    [Fact]
    public void ALobbyNeedsAValidListOfGames()
    {
        Assert.Throws<ValidationException>(() => LobbyRules.ValidateNewLobby(1, [], botCount: 0));
        Assert.Throws<ValidationException>(() => LobbyRules.ValidateNewLobby(1, [5, 5], botCount: 0));
        Assert.Throws<ValidationException>(() => LobbyRules.ValidateNewLobby(1, [.. Enumerable.Range(1, LobbyRules.MaxGames + 1)], botCount: 0));
    }

    [Fact]
    public void BotsAreLimited()
    {
        LobbyRules.ValidateNewLobby(1, [1], LobbyRules.MaxBots);
        Assert.Throws<ValidationException>(() => LobbyRules.ValidateNewLobby(1, [1], LobbyRules.MaxBots + 1));
    }
}
