using DraftPuck.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace DraftPuck.Web.Health;

/// <summary>
/// Two endpoints for the container platform and uptime monitoring:
/// <c>/health/live</c> says the process is up (it checks nothing else, so a database outage doesn't get the app
/// restarted), and <c>/health/ready</c> says it can serve requests, checking SQL Server and Redis.
/// </summary>
public static class HealthCheckExtensions
{
    private const string Ready = "ready";

    public static IServiceCollection AddDraftPuckHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<DraftPuckContext>("sql", tags: [Ready])
            .AddCheck<RedisHealthCheck>("redis", tags: [Ready]);

        return services;
    }

    public static IEndpointRouteBuilder MapDraftPuckHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(Ready) });
        return endpoints;
    }
}

public class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis didn't answer a ping.", ex);
        }
    }
}
