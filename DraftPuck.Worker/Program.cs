using DraftPuck.Application;
using DraftPuck.Infrastructure;
using DraftPuck.Shared.System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication();

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

// HttpClient logs four Information lines per request, and the NHL API is called constantly
// during games. Failures still surface as exceptions in the code that made the call.
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
// Azure.Core logs a warning for the expected 409 when CreateIfNotExists finds an existing queue or container.
builder.Logging.AddFilter("Azure.Core", LogLevel.Error);

builder.Services
    .Configure<ApplicationOptions>(options => builder.Configuration.Bind(ApplicationOptions.SectionName, options))
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddOptions<ApplicationOptions>()
    .Validate(o => o.HasValidBasePath(), "DRAFTPUCK_Application__BasePath must be set to an absolute URL (e.g. https://draftpuck.com).")
    .ValidateOnStart();

builder.Build().Run();
