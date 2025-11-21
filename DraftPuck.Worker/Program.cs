using DraftPuck.Application;
using DraftPuck.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication();

builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
