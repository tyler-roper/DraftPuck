using DraftPuck.Application;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Infrastructure;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Infrastructure.Persistence;
using DraftPuck.Infrastructure.SignalR;
using DraftPuck.Shared.Discord;
using DraftPuck.Web.Filters;
using DraftPuck.Web.Mvc.App;
using DraftPuck.Web.RateLimiting;
using DraftPuck.Web.Telemetry;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

//services
// With views: AppController serves the Vue app's page (Views/App/Index.cshtml).
builder.Services
    .AddControllersWithViews(options => { options.Filters.Add<ApiExceptionFilterAttribute>(); })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

try
{
    builder.Services
        .Configure<ApplicationOptions>(options => builder.Configuration.Bind(ApplicationOptions.SectionName, options))
        .Configure<DiscordOptions>(options => builder.Configuration.Bind(DiscordOptions.SectionName, options))
        .Configure<SystemOptions>(options => builder.Configuration.Bind(SystemOptions.SectionName, options))
        .AddHttpContextAccessor()
        .AddInfrastructure(builder.Configuration)
        .AddEndpointsApiExplorer()
        .AddApplication()
        .ConfigureAuth(builder.Configuration, builder.Environment)
        .AddTelemetry(builder.Configuration)
        .AddDraftPuckRateLimiting();

    builder.Services
        .AddOptions<ApplicationOptions>()
        .Validate(o => o.HasValidBasePath(), "DRAFTPUCK_Application__BasePath must be set to an absolute URL (e.g. https://draftpuck.com).")
        .ValidateOnStart();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}

ApplicationStartupInfo.Init();
builder.WebHost.UseKestrel();

//app
var app = builder.Build();

// First, so everything after (HTTPS redirection, rate limits, logged IPs) sees the client's address, not the proxy's.
app.UseForwardedHeaders();

MigrateDatabase(app);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts()
       .UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// In development, requests no endpoint handles (Vite's modules and assets) go to the Vite dev server.
if (app.Environment.IsDevelopment())
    app.UseWhen(
        context => context.GetEndpoint() is null,
        spaApp => spaApp.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("https://localhost:17010")));

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseWebSockets();

app.MapHub<LobbyHub>("/hub");
app.MapControllers();
// Client routes (any path that isn't a file) get the Vue app. Unknown /api paths don't: AppController.Api 404s them.
app.MapFallbackToController(nameof(AppController.Index), "App");

app.Run();


// Startup fails when migrations fail: the app must not run against a schema it doesn't match. EF Core holds a
// database-wide lock while migrating, so replicas starting together apply migrations one at a time.
static void MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        scope.ServiceProvider.GetRequiredService<DraftPuckContext>().Database.Migrate();
        app.Logger.LogInformation("Database migrations applied.");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Migrating the database failed; not starting.");
        throw;
    }
}
// Public so the integration tests can host the app with WebApplicationFactory<Program>.
public partial class Program;
