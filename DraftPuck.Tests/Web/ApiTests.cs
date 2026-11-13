using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace DraftPuck.Tests.Web;

/// <summary>Requests through the real HTTP pipeline: routing, authentication, authorization, the exception filter and SignalR.</summary>
[Collection(ApiCollection.Name)]
public class ApiTests(ApiFactory factory)
{
    private async Task<(string Jwt, Guid UserId, HttpClient Client)> NewGuestAsync()
    {
        var anonymous = factory.Client();
        var response = await anonymous.PostAsync("/api/users", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var jwt = body.GetProperty("jwtToken").GetString()!;
        var userId = body.GetProperty("user").GetProperty("id").GetGuid();
        return (jwt, userId, factory.Client(jwt));
    }

    private static async Task<string> CreateLobbyAsync(HttpClient client, string name = "Host")
    {
        var response = await client.PostAsJsonAsync("/api/lobbies", new { name, picksPerTeam = 1, gameIds = new[] { Seed.NewGameId() }, bots = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("joinCode").GetString()!;
    }

    [Theory]
    [InlineData("GET", "/api/lobbies")]
    [InlineData("POST", "/api/lobbies/ABCD/pick")]
    [InlineData("PATCH", "/api/users/00000000-0000-0000-0000-000000000001")]
    public async Task ProtectedEndpointsRejectRequestsWithoutAToken(string method, string path)
    {
        var response = await factory.Client().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectsTokensSignedWithAnotherKey()
    {
        var forged = new DraftPuck.Infrastructure.Auth.TokenService(new()
        {
            JwtKey = "a-different-signing-key-that-is-long-enough-too",
            Issuer = TestServices.AuthOptions.Issuer,
            Audience = TestServices.AuthOptions.Audience
        }).GenerateJwtToken(new UserEntity { Id = Guid.NewGuid(), IsAdmin = true });

        var response = await factory.Client(forged).GetAsync("/api/lobbies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AGuestCanStartPlayingRightAway()
    {
        var (_, _, client) = await NewGuestAsync();

        var code = await CreateLobbyAsync(client);
        var lobbies = await client.GetFromJsonAsync<JsonElement>("/api/lobbies");

        Assert.Contains(lobbies.EnumerateArray(), l => l.GetProperty("joinCode").GetString() == code);
    }

    [Fact]
    public async Task ErrorsBecomeProblemDetailsWithTheRightStatus()
    {
        var (_, _, host) = await NewGuestAsync();
        var (_, _, member) = await NewGuestAsync();
        var code = await CreateLobbyAsync(host);
        Assert.Equal(HttpStatusCode.OK, (await member.PostAsJsonAsync($"/api/lobbies/{code}/join", new { name = "Member" })).StatusCode);

        // 400: validation
        var invalid = await host.PostAsJsonAsync("/api/lobbies", new { name = "<script>", picksPerTeam = 1, gameIds = new[] { 1 }, bots = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("Name contains invalid characters.", (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());

        // 403: only the host can add bots
        var bot = await member.PostAsJsonAsync($"/api/lobbies/{code}/join", new { name = "Bot", isBot = true, botPickStyle = 0 });
        Assert.Equal(HttpStatusCode.Forbidden, bot.StatusCode);

        // 404: unknown lobby
        var missing = await member.PostAsJsonAsync("/api/lobbies/ZZZZ/pick", new { playerId = 1, gameId = 1, teamId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task AdminEndpointsRequireTheAdminRole()
    {
        var (_, _, guest) = await NewGuestAsync();
        UserEntity admin;
        await using (var db = factory.CreateDbContext())
        {
            admin = await SeedAccounts.AccountAsync(db, isAdmin: true);
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.Client(ApiFactory.JwtFor(admin)).GetAsync("/api/admin/users")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task InternalApiRequiresTheApiKey(string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/discord-server-joined")
        {
            Content = JsonContent.Create(new { discordUserId = "123" })
        };
        if (key != null)
            request.Headers.Add("X-Internal-Api-Key", key);

        var response = await factory.Client().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenCookieNeedsTheCsrfHeader()
    {
        var client = factory.Client();
        var guest = await (await client.PostAsync("/api/users", null)).Content.ReadFromJsonAsync<JsonElement>();
        var csrf = guest.GetProperty("antiCsrfToken").GetString()!;

        // The cookie alone (as a cross-site request would send it) is not enough.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/refreshTokens/use", null)).StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refreshTokens/use");
        request.Headers.Add("CSRF-Token", csrf);
        var refreshed = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.False(string.IsNullOrEmpty((await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("jwtToken").GetString()));
    }

    [Fact]
    public async Task HubRejectsConnectionsWithoutAToken()
    {
        await using var connection = factory.HubConnection(jwt: null);

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => connection.StartAsync());
    }

    [Fact]
    public async Task HubOnlyLetsMembersSubscribeToALobby()
    {
        var (hostJwt, _, host) = await NewGuestAsync();
        var (outsiderJwt, _, _) = await NewGuestAsync();
        var code = await CreateLobbyAsync(host);

        await using var member = factory.HubConnection(hostJwt);
        await member.StartAsync();
        await member.InvokeAsync("JoinLobby", code);

        await using var outsider = factory.HubConnection(outsiderJwt);
        await outsider.StartAsync();
        var error = await Assert.ThrowsAsync<HubException>(() => outsider.InvokeAsync("JoinLobby", code));
        Assert.Contains("not a member", error.Message);
    }
}
