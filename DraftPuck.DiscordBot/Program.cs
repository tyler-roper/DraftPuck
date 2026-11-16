using Azure.Monitor.OpenTelemetry.Exporter;
using DraftPuck.DiscordBot;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

// Logs go to Application Insights when APPLICATIONINSIGHTS_CONNECTION_STRING is set (in Azure; not locally).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Logging.AddOpenTelemetry(otel =>
    {
        otel.IncludeFormattedMessage = true;
        otel.AddAzureMonitorLogExporter();
    });
}

// The bot is a client of the web app's internal API: it needs its Discord settings and the API's address and key,
// and none of the app's database, cache or other services.
builder.Services
    .AddDiscordBotWorker(builder.Configuration)
    .AddInternalApiClient(builder.Configuration);

builder.Build().Run();
