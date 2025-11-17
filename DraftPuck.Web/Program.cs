using DraftPuck.Application;
using DraftPuck.Infrastructure;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Infrastructure.Persistence;
using DraftPuck.Web.Features.Lobbies;
using DraftPuck.Web.Filters;
using DraftPuck.Web.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

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

//services
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
});

try
{
    builder.Services
        .Configure<ApplicationOptions>(options => builder.Configuration.Bind(ApplicationOptions.SectionName, options))
        .AddHttpContextAccessor()
        .AddInfrastructure(builder.Configuration)
        .AddScoped<IClientEventService, LobbyClientEventService>()
        .AddEndpointsApiExplorer()
        .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LobbyEventCreatedHandler).Assembly))
        .AddApplication();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
        var key = Encoding.UTF8.GetBytes(authOptions!.JwtKey!);

        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

ApplicationStartupInfo.Init();
builder.WebHost.UseKestrel();
var app = builder.Build();

MigrateDatabase(app);

app
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseStaticFiles()
    .UseHsts()
    .UseHttpsRedirection()
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
{
    app.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer("https://localhost:17010"));
}

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