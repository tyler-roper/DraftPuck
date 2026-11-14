using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace DraftPuck.Web.RateLimiting;

/// <summary>
/// Per-endpoint request limits, applied with <c>[EnableRateLimiting(RateLimits.X)]</c>. Counts are kept in memory per
/// instance, so with several replicas the effective limit is that many times higher: a first line of defense against
/// floods and password guessing, not an exact quota.
/// </summary>
public static class RateLimits
{
    /// <summary>Password attempts, per IP: 10 a minute.</summary>
    public const string Login = "login";
    /// <summary>New guest accounts, per IP: 60 an hour (generous, since a whole bar can share one IP).</summary>
    public const string GuestCreation = "guest-creation";
    /// <summary>Chat messages, per user: 30 a minute.</summary>
    public const string Chat = "chat";

    public static IServiceCollection AddDraftPuckRateLimiting(this IServiceCollection services)
    {
        // The app runs behind the Container Apps ingress, so the client's address is in X-Forwarded-For. The app isn't
        // reachable except through that proxy, so the header is trusted from any address.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Slow down a little and try again shortly."
                }, ct);
            };

            options.AddPolicy(Login, context => FixedWindow(ClientIp(context), 10, TimeSpan.FromMinutes(1)));
            options.AddPolicy(GuestCreation, context => FixedWindow(ClientIp(context), 60, TimeSpan.FromHours(1)));
            options.AddPolicy(Chat, context => FixedWindow(UserIdOrIp(context), 30, TimeSpan.FromMinutes(1)));
        });
    }

    private static RateLimitPartition<string> FixedWindow(string key, int permits, TimeSpan window)
        => RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            QueueLimit = 0
        });

    /// <summary>The caller's IP, after the forwarded-headers middleware has applied X-Forwarded-For.</summary>
    public static string ClientIp(HttpContext context) => Format(context.Connection.RemoteIpAddress);

    private static string UserIdOrIp(HttpContext context)
        => context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId ? $"user:{userId}" : $"ip:{ClientIp(context)}";

    private static string Format(IPAddress? ip)
        => ip == null ? "unknown" : (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
}
