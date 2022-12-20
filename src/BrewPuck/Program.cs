using BrewPuck.Middleware;
using BrewPuck.Services.Hosted;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("BREWPUCK_");

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
builder.Services
    .AddDbContext<BrewPuckContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddEndpointsApiExplorer()
    .AddTransient<INotificationService, NotificationService>()
    .AddSingleton<IEventService, EventService>()
    .AddHostedService<KeepAliveService>();

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
    .UseMvc(routes =>
    {
        routes.MapRoute(name: "default", template: "{controller=App}/{action=Index}/{id?}");
        routes.MapSpaFallbackRoute("spa-routes", new { controller = "App", action = "Index" });
    })
    .UseAuthentication()
    .UseEndpoints(endpoints => { endpoints.MapControllers(); });

if (app.Environment.IsDevelopment())
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("https://localhost:17010"));

app.Run();