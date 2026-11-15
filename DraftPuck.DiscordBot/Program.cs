using DraftPuck.DiscordBot;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("DRAFTPUCK_");

// The bot is a client of the web app's internal API: it needs its Discord settings and the API's address and key,
// and none of the app's database, cache or other services.
builder.Services
    .AddDiscordBotWorker(builder.Configuration)
    .AddInternalApiClient(builder.Configuration);

builder.Build().Run();
