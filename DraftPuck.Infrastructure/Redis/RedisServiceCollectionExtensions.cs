using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DraftPuck.Infrastructure.Redis;
public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            if (string.IsNullOrEmpty(options?.ConnectionString))
                throw new InvalidOperationException("Redis Connection String is required but missing.");

            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        return services.AddSingleton(sp =>
            sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
    }
}
