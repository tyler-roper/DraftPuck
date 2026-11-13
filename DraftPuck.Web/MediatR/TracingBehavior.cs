using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Web.MediatR;

public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly ActivitySource Source = new("MediatR");
    private readonly ILogger<TracingBehavior<TRequest, TResponse>> _logger;

    public TracingBehavior(ILogger<TracingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        using var activity = Source.StartActivity(typeof(TRequest).Name, ActivityKind.Internal);

        try
        {
            _logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
            var response = await next(ct);
            _logger.LogDebug("Handled {RequestType}", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            // Not logged here: every request comes from a controller, and ApiExceptionFilterAttribute
            // logs the exception at the right level once. Logging here too duplicated every error.
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
