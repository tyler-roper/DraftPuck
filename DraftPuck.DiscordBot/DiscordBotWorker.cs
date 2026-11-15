using Discord;
using Discord.WebSocket;
using DraftPuck.Application.Features.Discord;
using DraftPuck.DiscordBot.Services;
using Microsoft.Extensions.Options;

namespace DraftPuck.DiscordBot;

public class DiscordBotWorker(
    DiscordSocketClient client,
    DiscordEventHandler handler,
    IOptions<DiscordOptions> discordConfig,
    ILogger<DiscordBotWorker> logger) : BackgroundService
{
    private readonly DiscordOptions _discordConfig = discordConfig.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Discord.Net reports its own problems, including exceptions thrown by event handlers, only through this event.
        client.Log += OnDiscordLogAsync;
        client.UserJoined += user => handler.OnUserJoinedAsync(user, stoppingToken);
        client.Ready += OnReadyAsync;
        client.InteractionCreated += OnInteractionAsync;

        await client.LoginAsync(TokenType.Bot, _discordConfig.BotToken);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReadyAsync()
    {
        var guild = client.GetGuild(ulong.Parse(_discordConfig.GuildId!));

        var verifyCommand = new SlashCommandBuilder()
            .WithName("verify")
            .WithDescription("Verify your DraftPuck account");

        await guild.CreateApplicationCommandAsync(verifyCommand.Build());

        logger.LogInformation("Slash command /verify registered");
    }

    private async Task OnInteractionAsync(SocketInteraction interaction)
    {
        if (interaction is SocketSlashCommand slashCommand)
            await handler.HandleSlashCommandAsync(slashCommand);
    }

    private Task OnDiscordLogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            _ => LogLevel.Trace
        };

        logger.Log(level, message.Exception, "Discord {Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
