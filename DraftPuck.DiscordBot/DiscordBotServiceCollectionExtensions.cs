using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;
using DraftPuck.DiscordBot.Services;
using DraftPuck.Shared.Discord;
using DraftPuck.Shared.System;
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
            .AddSingleton(sp => new DiscordSocketClient(new DiscordSocketConfig() { 
                GatewayIntents = Discord.GatewayIntents.Guilds
                               | Discord.GatewayIntents.GuildMessages
                               | Discord.GatewayIntents.MessageContent 
            }))
            .AddSingleton<DiscordEventHandler>()
            .Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName))
            .AddHostedService<DiscordBotWorker>();
    }

}
