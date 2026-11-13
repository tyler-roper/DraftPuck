using System.Net.Http.Headers;
using DraftPuck.Application.Features.Games;
using DraftPuck.Shared.Achievements;
using DraftPuck.Shared.Firebase;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace DraftPuck.Tests.Support;

/// <summary>
/// Hosts the real web app (Program, middleware, auth, filters, SignalR) against its own SQL Server and Redis
/// containers. External services (Firebase, Azure Storage queues and blobs, the NHL API) are replaced with fakes.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string InternalApiKey = "test-internal-api-key";

    private readonly MsSqlContainer _sql = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7.4-alpine").Build();

    public IPushNotificationService Push { get; } = Substitute.For<IPushNotificationService>();
    public INhlClient Nhl { get; } = Substitute.For<INhlClient>();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());
        _ = Server; // start the app now, which applies the migrations
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _sql.GetConnectionString());
        builder.UseSetting("Redis:ConnectionString", _redis.GetConnectionString());
        builder.UseSetting("Auth:JwtKey", TestServices.AuthOptions.JwtKey);
        builder.UseSetting("Auth:Issuer", TestServices.AuthOptions.Issuer);
        builder.UseSetting("Auth:Audience", TestServices.AuthOptions.Audience);
        builder.UseSetting("Application:BasePath", "https://draftpuck.test");
        builder.UseSetting("Application:InternalApiKey", InternalApiKey);

        builder.ConfigureTestServices(services =>
        {
            // The in-memory test server has no socket, so give requests a client IP like a real connection has.
            services.AddSingleton<IStartupFilter, ClientIpStartupFilter>();

            Replace(services, Substitute.For<IAchievementQueueService>());
            Replace(services, Substitute.For<INhlQueueService>());
            Replace(services, Substitute.For<IAvatarStorageService>());
            Replace(services, Push);
            Replace(services, Nhl);
        });
    }

    private static void Replace<T>(IServiceCollection services, T instance) where T : class
    {
        services.RemoveAll<T>();
        services.AddSingleton(instance);
    }

    public DraftPuckContext CreateDbContext() => new(
        new DbContextOptionsBuilder<DraftPuckContext>().UseSqlServer(_sql.GetConnectionString()).Options);

    /// <summary>A client over HTTPS (the refresh cookie is Secure), optionally sending a bearer token.</summary>
    public HttpClient Client(string? jwt = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (jwt != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    public static string JwtFor(UserEntity user) => TestServices.TokenService().GenerateJwtToken(user);
    public static string GuestJwtFor(Guid userId) => TestServices.TokenService().GenerateGuestJwtToken(userId);

    public HubConnection HubConnection(string? jwt) => new HubConnectionBuilder()
        .WithUrl(new Uri(Server.BaseAddress, "hub"), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            if (jwt != null)
                options.AccessTokenProvider = () => Task.FromResult<string?>(jwt);
        })
        .Build();
}

internal sealed class ClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= System.Net.IPAddress.Parse("203.0.113.7");
            return nextMiddleware(context);
        });
        next(app);
    };
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
