namespace DraftPuck.Infrastructure.RateLimiting;
public class ApiRateLimiter(int minDelayMilliseconds) : IApiRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTime _lastCallUtc = DateTime.MinValue;

    public async Task WaitForPermitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastCallUtc).TotalMilliseconds;

            if (elapsed < minDelayMilliseconds)
            {
                var delay = minDelayMilliseconds - (int)elapsed;
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            _lastCallUtc = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}