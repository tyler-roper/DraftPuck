using Azure.Monitor.OpenTelemetry.AspNetCore;
using DraftPuck.Web.MediatR;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DraftPuck.Web.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Sends requests, dependencies, metrics and logs to Application Insights through the Azure Monitor OpenTelemetry
    /// distro, when a connection string is configured. Logs go at the levels in appsettings' Logging section.
    /// </summary>
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TelemetryOptions>(config.GetSection(TelemetryOptions.SectionName));
        var options = config.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("DraftPuck.Web"))
            .UseAzureMonitor(o =>
            {
                o.ConnectionString = options.ConnectionString;
                o.SamplingRatio = options.SampleRate;
            })
            .WithTracing(tracer => tracer
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("MediatR")
                .AddSource("DraftPuck"))
            .WithMetrics(metrics => metrics.AddRuntimeInstrumentation());

        // Container probes call these every few seconds; recording them would bury real traffic.
        services.Configure<AspNetCoreTraceInstrumentationOptions>(o =>
        {
            o.RecordException = true;
            o.Filter = context => !context.Request.Path.StartsWithSegments("/health");
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        return services;
    }
}
