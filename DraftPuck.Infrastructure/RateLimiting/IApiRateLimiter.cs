namespace DraftPuck.Infrastructure.RateLimiting;
public interface IApiRateLimiter
{
    Task WaitForPermitAsync(CancellationToken cancellationToken = default);
}