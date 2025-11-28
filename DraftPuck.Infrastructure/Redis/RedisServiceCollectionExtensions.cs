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

        services.AddSignalR()
                .AddStackExchangeRedis(o =>
                {
                    o.ConnectionFactory = async writer => multiplexer;
                    o.Configuration.ChannelPrefix = RedisChannel.Literal("DraftPuckSignalR");
                });

        services.AddScoped<IClientEventService, LobbyClientEventService>();

        return services;
    }
}
