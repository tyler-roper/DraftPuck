using Discord;
using Discord.WebSocket;
using DraftPuck.DiscordBot.Services;
using DraftPuck.Shared.Discord;
using Microsoft.Extensions.Options;

namespace DraftPuck.DiscordBot;

public class DiscordBotWorker(DiscordSocketClient client, DiscordEventHandler handler, IOptions<DiscordOptions> discordConfig) : BackgroundService
{
    private readonly DiscordOptions _discordConfig = discordConfig.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            client.UserJoined += async user => await handler.OnUserJoinedAsync(user, stoppingToken);
            client.Ready += OnReadyAsync;
            client.InteractionCreated += OnInteractionAsync;

            await client.LoginAsync(TokenType.Bot, _discordConfig.BotToken);
            await client.StartAsync();

            await Task.Delay(-1, stoppingToken); // Keep running
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private async Task OnReadyAsync()
    {
        var guild = client.GetGuild(ulong.Parse(_discordConfig.GuildId!));

        var verifyCommand = new SlashCommandBuilder()
            .WithName("verify")
            .WithDescription("Verify your DraftPuck account");

        await guild.CreateApplicationCommandAsync(verifyCommand.Build());

        Console.WriteLine("Slash command '/verify' registered.");
    }

    private async Task OnInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            if (interaction is SocketSlashCommand slashCommand)
                await handler.HandleSlashCommandAsync(slashCommand);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Interaction error: " + ex);
        }
    }
}