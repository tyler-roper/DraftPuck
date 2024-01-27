using Draftpuck.NhlApi.Services;
using Draftpuck.NhlApi.Services.Interfaces;
using DraftPuck.Core.Services;
using DraftPuck.Data.Data;
using DraftPuck.SignalR.Hubs;
using DraftPuck.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddMvc(options =>
{
    options.EnableEndpointRouting = false;
});

builder.Logging.AddConsole();

//services
builder.Services.AddSignalR();
builder.Services.AddHttpClient<INhlApiService, NhlApiService>(client => client.BaseAddress = new Uri("https://api-web.nhle.com/v1/"));
builder.Services
    .AddDbContext<DraftPuckContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddHostedService<GameCheckerHostedService>()
    .AddHostedService<LobbyCleanupHostedService>()
    .AddSingleton<IGameCache, GameCache>()
    .AddTransient<IGameService, GameService>()
    .AddTransient<ILobbyService, LobbyService>()
    .AddTransient<ILobbyEventService, LobbyEventService>()
    .AddTransient<INhlService, NhlService>()
    .AddTransient<IUserService, UserService>()
    .AddEndpointsApiExplorer()
    .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//kestrel
builder.WebHost.UseKestrel();

//build
var app = builder.Build();

//this order matters!
app
    .UseMiddleware<UserMiddleware>()
    .UseRouting()
    .UseStaticFiles()
    .UseHsts()
    .UseHttpsRedirection()
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
{
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("https://localhost:17010"));
}

app.Run();