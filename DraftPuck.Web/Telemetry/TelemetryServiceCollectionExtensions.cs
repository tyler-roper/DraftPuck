using Azure.Monitor.OpenTelemetry.Exporter;
using DraftPuck.Web.MediatR;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DraftPuck.Web.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration config)
    {
        var telemetryOptions = config.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();
        services.Configure<TelemetryOptions>(config.GetSection(TelemetryOptions.SectionName));

        var isProd = config.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Production";
        if (!isProd || !telemetryOptions.EnableTracing) return services;

        var serviceName = "DraftPuck";
        var serviceVersion = "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracer =>
            {
                tracer
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options => options.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("MediatR")
                    .AddSource("DraftPuck")
                    .AddAzureMonitorTraceExporter(o =>
                    {
                        o.ConnectionString = telemetryOptions.ConnectionString;
                    });

                tracer.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(telemetryOptions.SampleRate)));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAzureMonitorMetricExporter(o =>
                    {
                        o.ConnectionString = telemetryOptions.ConnectionString;
                    });
            });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        return services;
    }
}