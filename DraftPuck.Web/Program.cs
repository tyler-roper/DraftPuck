using DraftPuck.Application;
using DraftPuck.Infrastructure;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Infrastructure.Persistence;
using DraftPuck.Shared.Discord;
using DraftPuck.Web.Features.Lobbies;
using DraftPuck.Web.Filters;
using DraftPuck.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

//services
builder.Services
    .AddControllers(options => { options.Filters.Add<ApiExceptionFilterAttribute>(); })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddMvc(options =>
{
    options.EnableEndpointRouting = false;
});


builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
});

try
{
    builder.Services
        .Configure<ApplicationOptions>(options => builder.Configuration.Bind(ApplicationOptions.SectionName, options))
        .Configure<DiscordOptions>(options => builder.Configuration.Bind(DiscordOptions.SectionName, options))
        .Configure<SystemOptions>(options => builder.Configuration.Bind(SystemOptions.SectionName, options))
        .AddHttpContextAccessor()
        .AddInfrastructure(builder.Configuration)
        .AddScoped<IClientEventService, LobbyClientEventService>()
        .AddEndpointsApiExplorer()
        .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LobbyEventCreatedHandler).Assembly))
        .AddApplication()
        .ConfigureAuth(builder.Configuration, builder.Environment);
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

MigrateDatabase(app);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts()
       .UseHttpsRedirection();
}

app
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseStaticFiles()
    .UseWebSockets()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapHub<LobbyHub>("/hub");
        endpoints.MapControllers();
    })
    .UseMvc(routes =>
    {
        routes.MapRoute(name: "default", template: "{controller=App}/{action=Index}/{id?}");
        routes.MapSpaFallbackRoute("spa-routes", new { controller = "App", action = "Index" });
    });


if (app.Environment.IsDevelopment())
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("https://localhost:17010"));

app.Run();


static void MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DraftPuckContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}