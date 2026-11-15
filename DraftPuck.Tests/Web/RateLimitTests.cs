using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Tests.Web;

/// <summary>Request limits, through the real pipeline. Each test client has its own client IP (see ApiFactory.Client).</summary>
[Collection(ApiCollection.Name)]
public class RateLimitTests(ApiFactory factory)
{
    [Fact]
    public async Task LoginAttemptsAreLimitedPerIp()
    {
        var client = factory.Client();
        var login = () => client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong" });

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await login()).StatusCode);

        var limited = await login();
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.Contains("Retry-After"));

        // Someone else, from another address, isn't affected.
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client().PostAsJsonAsync("/api/auth/login", new { email = "x@example.com", password = "wrong" })).StatusCode);
    }

    [Fact]
    public async Task GuestCreationIsLimitedPerIp()
    {
        const string sharedIp = "192.0.2.10";

        for (var i = 0; i < 60; i++)
            Assert.Equal(HttpStatusCode.Created, (await factory.Client(clientIp: sharedIp).PostAsync("/api/users", null)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await factory.Client(clientIp: sharedIp).PostAsync("/api/users", null)).StatusCode);
    }

    [Fact]
    public async Task ChatIsLimitedPerUser()
    {
        var guest = await factory.Client().PostAsync("/api/users", null);
        var jwt = (await guest.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("jwtToken").GetString()!;
        var client = factory.Client(jwt);
        var lobby = await client.PostAsJsonAsync("/api/lobbies", new { name = "Host", picksPerTeam = 1, gameIds = new[] { Seed.NewGameId() }, bots = Array.Empty<object>() });
        var code = (await lobby.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("joinCode").GetString()!;

        for (var i = 0; i < 30; i++)
            Assert.True((await client.PostAsJsonAsync($"/api/lobbies/{code}/message", new { message = $"msg {i}" })).IsSuccessStatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync($"/api/lobbies/{code}/message", new { message = "one too many" })).StatusCode);
    }

    [Fact]
    public async Task TheClientIpComesFromTheProxyHeader()
    {
        // Refresh tokens record the client's address; behind the ingress that must be the visitor's, not the proxy's.
        var response = await factory.Client(clientIp: "203.0.113.7").PostAsync("/api/users", null);
        var userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DraftPuckContext>();
        Assert.Equal("203.0.113.7", (await db.UserRefreshTokens.SingleAsync(t => t.UserId == userId)).CreatedByIp);
    }
}
