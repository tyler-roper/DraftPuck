using Discord.WebSocket;
using DraftPuck.Application.Common.Options;
using DraftPuck.Application.Features.Discord;
using DraftPuck.DiscordBot.Interfaces;
using DraftPuck.DiscordBot.Services;
using DraftPuck.Domain.System;
using Microsoft.Extensions.Options;

namespace DraftPuck.DiscordBot;

public static class DiscordBotServiceCollectionExtensions
{
    public static void AddInternalApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<ApplicationOptions>(configuration.GetSection(ApplicationOptions.SectionName))
            .AddHttpClient<IInternalApiClient, InternalApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ApplicationOptions>>().Value;
                client.BaseAddress = new Uri(options.InternalApiBasePath ?? options.BasePath);
                client.DefaultRequestHeaders.Add("X-Internal-Api-Key", options.InternalApiKey);
            });
    }

    public static IServiceCollection AddDiscordBotWorker(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            // Slash commands only need Guilds. UserJoined events also need GuildMembers, a privileged intent that has to
            // be enabled for the bot in the Discord developer portal before it's requested here.
            .AddSingleton(sp => new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = Discord.GatewayIntents.Guilds
            }))
            .AddSingleton<DiscordEventHandler>()
            .Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName))
            .AddHostedService<DiscordBotWorker>();
    }

}
