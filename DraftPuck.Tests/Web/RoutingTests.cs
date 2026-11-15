using System.Net;

namespace DraftPuck.Tests.Web;

/// <summary>
/// How paths that aren't API endpoints are routed: unknown API paths are a 404 (not the Vue app), and client routes
/// get the Vue app's page so the router can take over.
/// </summary>
[Collection(ApiCollection.Name)]
public class RoutingTests(ApiFactory factory)
{
    [Theory]
    [InlineData("GET", "/api/does-not-exist")]
    [InlineData("GET", "/api/lobbies/ABCD/does-not-exist")]
    [InlineData("POST", "/api/does-not-exist")]
    [InlineData("DELETE", "/api/nothing/here/either")]
    public async Task UnknownApiPathsAreNotFound(string method, string path)
    {
        var response = await factory.Client().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/lobby/ABCD")]
    [InlineData("/lobbies")]
    [InlineData("/some/deep/client/route")]
    public async Task ClientRoutesServeTheVueApp(string path)
    {
        var response = await factory.Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>DRAFTPUCK - A live hockey drinking game.</title>", html);
        Assert.Contains("<div id=\"app\"", html);
    }

    [Fact]
    public async Task MissingFilesAreNotServedTheVueApp()
    {
        var response = await factory.Client().GetAsync("/assets/does-not-exist.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
