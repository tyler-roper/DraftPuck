using System.Net;
using DraftPuck.Web.Security;

namespace DraftPuck.Tests.Web;

/// <summary>The security headers every response carries, and the health endpoints the platform probes.</summary>
[Collection(ApiCollection.Name)]
public class SecurityAndHealthTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/lobby/ABCD")]
    [InlineData("/api/does-not-exist")]
    [InlineData("/favicon.ico")]
    public async Task EveryResponseCarriesTheSecurityHeaders(string path)
    {
        var response = await factory.Client().GetAsync(path);

        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public void ThePolicyAllowsNoInlineOrEvaluatedScript()
    {
        var scriptSrc = SecurityHeaders.ContentSecurityPolicy.Split(';').Select(d => d.Trim()).Single(d => d.StartsWith("script-src "));

        Assert.DoesNotContain("'unsafe-inline'", scriptSrc);
        Assert.DoesNotContain("'unsafe-eval'", scriptSrc);
        Assert.Contains("frame-ancestors 'none'", SecurityHeaders.ContentSecurityPolicy);
        Assert.Contains("object-src 'none'", SecurityHeaders.ContentSecurityPolicy);
    }

    [Fact]
    public void TheAppPageHasNoInlineScripts()
    {
        // The policy forbids them, so one in the page would silently not run. The source index.html is checked: the
        // build turns it into Views/App/Index.cshtml.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "DraftPuck.sln")))
            directory = directory.Parent ?? throw new InvalidOperationException("Couldn't find the repository root.");

        var html = File.ReadAllText(Path.Combine(directory.FullName, "DraftPuck.Web", "ClientApp", "index.html"));

        Assert.Matches(@"<script\b", html);
        Assert.DoesNotMatch(@"<script(?![^>]*\bsrc=)[^>]*>", html);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsReportHealthyWhenSqlAndRedisAreUp(string path)
    {
        var response = await factory.Client().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
