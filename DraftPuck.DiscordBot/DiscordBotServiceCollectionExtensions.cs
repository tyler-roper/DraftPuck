using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;
using DraftPuck.DiscordBot.Services;
using Microsoft.Extensions.Options;

namespace DraftPuck.DiscordBot;

public static class DiscordBotServiceCollectionExtensions
{
    public static IServiceCollection AddInternalApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InternalApiOptions>()
            .Bind(configuration.GetSection(InternalApiOptions.SectionName))
            .Validate(o => Uri.IsWellFormedUriString(o.BaseAddress, UriKind.Absolute),
                "DRAFTPUCK_Application__InternalApiBasePath (or BasePath) must be set to the web app's absolute URL.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.InternalApiKey), "DRAFTPUCK_Application__InternalApiKey must be set.")
            .ValidateOnStart();

        services.AddHttpClient<IInternalApiClient, InternalApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<InternalApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress!);
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", options.InternalApiKey);
        });

        return services;
    }

    public static IServiceCollection AddDiscordBotWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DiscordBotOptions>()
            .Bind(configuration.GetSection(DiscordBotOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BotToken), "DRAFTPUCK_Discord__BotToken must be set.")
            .Validate(o => ulong.TryParse(o.GuildId, out _), "DRAFTPUCK_Discord__GuildId must be set to the server's ID.")
            .ValidateOnStart();

        return services
            // Slash commands need Guilds; UserJoined events need GuildMembers, a privileged intent. It's enabled for the
            // bot in the Discord developer portal (Privileged Gateway Intents → Server Members Intent); if it's ever
            // turned off there, Discord refuses the connection and the bot doesn't start.
            .AddSingleton(sp => new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = Discord.GatewayIntents.Guilds | Discord.GatewayIntents.GuildMembers
            }))
            .AddSingleton<DiscordEventHandler>()
            .AddHostedService<DiscordBotWorker>();
    }
}
