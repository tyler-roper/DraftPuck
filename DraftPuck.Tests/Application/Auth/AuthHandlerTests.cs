using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Auth;

namespace DraftPuck.Tests.Application.Auth;

[Collection(DatabaseCollection.Name)]
public class AuthHandlerTests(DatabaseFixture fixture)
{
    private const string Ip = "203.0.113.7";
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private async Task<AuthenticationResultDto> LoginAsync(string email, string password, Guid guestUserId = default)
    {
        await using var db = fixture.CreateContext();
        var handler = new LoginCommandHandler(db, TestServices.TokenService(), new UserRepository(db), TestServices.Mapper(), _mediator);
        return await handler.Handle(new LoginCommand { Email = email, Password = password, IpAddress = Ip, GuestUserId = guestUserId }, default);
    }

    private async Task<AuthenticationResultDto> RefreshAsync(string refreshToken, string? csrf)
    {
        await using var db = fixture.CreateContext();
        var handler = new UseRefreshTokenCommandHandler(db, TestServices.TokenService(), new UserRepository(db), TestServices.Mapper());
        return await handler.Handle(new UseRefreshTokenCommand { RefreshToken = refreshToken, AntiCsrfTokenHeader = csrf, IpAddress = Ip }, default);
    }

    private async Task<UserEntity> AccountAsync(string? nickname = null)
    {
        await using var db = fixture.CreateContext();
        return await SeedAccounts.AccountAsync(db, nickname);
    }

    // --- Login ---

    [Fact]
    public async Task LoginWithCorrectPasswordReturnsTokens()
    {
        var account = await AccountAsync();

        var result = await LoginAsync(account.Email!, SeedAccounts.Password);

        Assert.Equal(account.Id, result.User.Id);
        Assert.False(string.IsNullOrEmpty(result.JwtToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task LoginRejectsWrongPasswordAndUnknownEmail()
    {
        var account = await AccountAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(account.Email!, "wrong-password"));
        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync("nobody@example.com", SeedAccounts.Password));
    }

    [Fact]
    public async Task LoginRejectsDeactivatedAccounts()
    {
        var account = await AccountAsync();
        await using (var db = fixture.CreateContext())
        {
            await db.Users.Where(u => u.Id == account.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
        }

        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(account.Email!, SeedAccounts.Password));
    }

    [Fact]
    public async Task LoginMovesTheGuestsLobbiesToTheAccount()
    {
        var account = await AccountAsync();
        Guid guestId, memberId, lobbyId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, guestHost) = seed.Lobby([Seed.NewGameId()]);
            guestId = guestHost.UserId;
            memberId = guestHost.Id;
            lobbyId = lobby.Id;
            await db.SaveChangesAsync();
        }

        await LoginAsync(account.Email!, SeedAccounts.Password, guestId);

        await using var check = fixture.CreateContext();
        var member = await check.LobbyMembers.SingleAsync(m => m.Id == memberId);
        Assert.Equal(account.Id, member.UserId);
        Assert.Equal(account.Nickname, member.Name);
        Assert.Equal(account.Id, (await check.Lobbies.SingleAsync(l => l.Id == lobbyId)).CreatedBy);
    }

    [Fact]
    public async Task LoginRemovesTheGuestFromLobbiesTheAccountIsAlreadyIn()
    {
        var account = await AccountAsync();
        Guid guestMemberId, guestId, guestPickId, accountMemberId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var gameId = Seed.NewGameId();
            var (lobby, _) = seed.Lobby([gameId]);
            var guestMember = seed.Member(lobby);
            guestId = guestMember.UserId;
            guestMemberId = guestMember.Id;
            guestPickId = seed.Pick(guestMember, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0)).Id;
            accountMemberId = seed.Member(lobby, await db.Users.SingleAsync(u => u.Id == account.Id)).Id;
            await db.SaveChangesAsync();
        }

        await LoginAsync(account.Email!, SeedAccounts.Password, guestId);

        await using var check = fixture.CreateContext();
        Assert.True((await check.LobbyMembers.SingleAsync(m => m.Id == guestMemberId)).IsRemoved);
        Assert.False((await check.LobbyMemberPicks.SingleAsync(p => p.Id == guestPickId)).IsActive);
        Assert.False((await check.LobbyMembers.SingleAsync(m => m.Id == accountMemberId)).IsRemoved);
    }

    [Fact]
    public async Task LoginDoesNotTakeLobbiesFromThePreviouslySignedInAccount()
    {
        var previous = await AccountAsync();
        var account = await AccountAsync();
        Guid memberId, lobbyId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var previousUser = await db.Users.SingleAsync(u => u.Id == previous.Id);
            var (lobby, _) = seed.Lobby([Seed.NewGameId()]);
            lobby.CreatedByUser = previousUser; // the previous account hosts the lobby
            lobby.CreatedBy = previous.Id;
            memberId = seed.Member(lobby, previousUser).Id;
            lobbyId = lobby.Id;
            await db.SaveChangesAsync();
        }

        await LoginAsync(account.Email!, SeedAccounts.Password, guestUserId: previous.Id);

        await using var check = fixture.CreateContext();
        Assert.Equal(previous.Id, (await check.LobbyMembers.SingleAsync(m => m.Id == memberId)).UserId);
        Assert.Equal(previous.Id, (await check.Lobbies.SingleAsync(l => l.Id == lobbyId)).CreatedBy);
    }

    [Fact]
    public async Task LoginRenamesAnotherMemberAlreadyUsingTheAccountsName()
    {
        var account = await AccountAsync();
        Guid guestId, otherMemberId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([Seed.NewGameId()]);
            var guest = seed.Member(lobby);
            guestId = guest.UserId;
            var other = seed.Member(lobby);
            other.Name = account.Nickname!.ToUpperInvariant();
            otherMemberId = other.Id;
            await db.SaveChangesAsync();
        }

        await LoginAsync(account.Email!, SeedAccounts.Password, guestId);

        await using var check = fixture.CreateContext();
        Assert.Equal(account.Nickname!.ToUpperInvariant() + "2", (await check.LobbyMembers.SingleAsync(m => m.Id == otherMemberId)).Name);
    }

    // --- Refresh tokens ---

    [Fact]
    public async Task RefreshRotatesTheToken()
    {
        var account = await AccountAsync();
        var login = await LoginAsync(account.Email!, SeedAccounts.Password);

        var refreshed = await RefreshAsync(login.RefreshToken, login.AntiCsrfToken.ToString());

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken, login.AntiCsrfToken.ToString()));
    }

    [Fact]
    public async Task ReusingARotatedTokenRevokesTheWholeChain()
    {
        // A stolen, already-used token must also kill the token that replaced it.
        var account = await AccountAsync();
        var login = await LoginAsync(account.Email!, SeedAccounts.Password);
        var refreshed = await RefreshAsync(login.RefreshToken, login.AntiCsrfToken.ToString());

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken, login.AntiCsrfToken.ToString()));

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(refreshed.RefreshToken, refreshed.AntiCsrfToken.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task RefreshRequiresTheMatchingCsrfToken(string? csrf)
    {
        var account = await AccountAsync();
        var login = await LoginAsync(account.Email!, SeedAccounts.Password);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(login.RefreshToken, csrf));

        // The failed attempt didn't consume the token.
        await RefreshAsync(login.RefreshToken, login.AntiCsrfToken.ToString());
    }

    [Fact]
    public async Task RefreshRejectsUnknownTokens()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("not-a-real-token", Guid.NewGuid().ToString()));
    }

    // --- Logout and revoke ---

    [Fact]
    public async Task LogoutRevokesEverySession()
    {
        var account = await AccountAsync();
        var phone = await LoginAsync(account.Email!, SeedAccounts.Password);
        var laptop = await LoginAsync(account.Email!, SeedAccounts.Password);

        await using (var db = fixture.CreateContext())
        {
            await new LogoutCommandHandler(db).Handle(new LogoutCommand { UserId = account.Id }, default);
        }

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(phone.RefreshToken, phone.AntiCsrfToken.ToString()));
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(laptop.RefreshToken, laptop.AntiCsrfToken.ToString()));
    }

    [Fact]
    public async Task RevokeEndsOneSessionAndCannotBeRepeated()
    {
        var account = await AccountAsync();
        var phone = await LoginAsync(account.Email!, SeedAccounts.Password);
        var laptop = await LoginAsync(account.Email!, SeedAccounts.Password);

        async Task Revoke()
        {
            await using var db = fixture.CreateContext();
            await new RevokeTokenCommandHandler(db, new UserRepository(db)).Handle(new RevokeTokenCommand { Token = phone.RefreshToken, IpAddress = Ip }, default);
        }

        await Revoke();

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(phone.RefreshToken, phone.AntiCsrfToken.ToString()));
        await RefreshAsync(laptop.RefreshToken, laptop.AntiCsrfToken.ToString());
        await Assert.ThrowsAsync<BadRequestException>(Revoke);
    }
}
