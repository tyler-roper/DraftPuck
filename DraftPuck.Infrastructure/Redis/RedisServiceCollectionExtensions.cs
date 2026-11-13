using DraftPuck.Infrastructure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DraftPuck.Infrastructure.Redis;
public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisAndSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(section);

        var options = section.Get<RedisOptions>();
        if (string.IsNullOrEmpty(options?.ConnectionString))
            throw new InvalidOperationException("Redis Connection String is required but missing.");

        var multiplexer = ConnectionMultiplexer.Connect(options.ConnectionString);
        services.AddSingleton(multiplexer);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddSingleton(sp => multiplexer.GetDatabase());

        // Mobile browsers throttle the JS timers that send client pings, so allow well beyond
        // the 30s default before treating a client as gone.
        services.AddSignalR(o =>
                {
                    o.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    o.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
                })
                .AddStackExchangeRedis(o =>
                {
                    o.ConnectionFactory = async writer => multiplexer;
                    o.Configuration.ChannelPrefix = RedisChannel.Literal("DraftPuckSignalR");
                });

        services.AddScoped<IClientEventService, LobbyClientEventService>();

        return services;
    }
}
