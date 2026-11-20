using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DraftPuck.Tests.Web;

/// <summary>The API's OpenAPI document and Swagger UI: served in Development, for contributors, and nowhere else.</summary>
[Collection(ApiCollection.Name)]
public class OpenApiTests(ApiFactory factory)
{
    private static readonly WebApplicationFactoryClientOptions Options = new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false };

    [Fact]
    public async Task DevelopmentServesTheDocumentAndTheUi()
    {
        using var development = factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var client = development.CreateClient(Options);

        var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json")).RootElement;
        Assert.True(document.TryGetProperty("openapi", out _));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/Games/{gameId}", out _));

        Assert.Contains("swagger-ui", await client.GetStringAsync("/swagger/index.html"));
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task OtherEnvironmentsDoNot(string path)
    {
        var response = await factory.Client().GetAsync(path);

        Assert.DoesNotContain("openapi", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("swagger-ui", await response.Content.ReadAsStringAsync());
    }
}
