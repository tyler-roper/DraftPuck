using DraftPuck.Application;
using DraftPuck.DiscordBot;
using DraftPuck.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

builder
    .Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddDiscordBotWorker(builder.Configuration)
    .AddInternalApiClient(builder.Configuration);

builder.Build().Run();