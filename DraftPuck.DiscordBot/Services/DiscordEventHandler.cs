using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;
using Microsoft.Extensions.Options;

namespace DraftPuck.DiscordBot.Services;

public class DiscordEventHandler(IInternalApiClient apiClient, IOptions<DiscordBotOptions> discordConfig, ILogger<DiscordEventHandler> logger)
{
    private readonly ulong _guildId = ulong.Parse(discordConfig.Value.GuildId!);

    /// <summary>Someone joined a server the bot is in. Joining DraftPuck's server earns the Discord achievement, if
    /// their Discord account is linked; members who joined before linking run /verify instead.</summary>
    public async Task OnUserJoinedAsync(SocketGuildUser user, CancellationToken ct)
    {
        if (user.Guild.Id != _guildId || user.IsBot)
            return;

        try
        {
            await apiClient.SendDiscordServerJoinedNotification(user.Id.ToString(), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Telling the web app Discord user {DiscordUserId} joined failed", user.Id);
        }
    }

    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "verify")
            return;

        await command.DeferAsync(ephemeral: true);

        try
        {
            await apiClient.SendDiscordServerJoinedNotification(command.User.Id.ToString(), CancellationToken.None);
            await command.FollowupAsync($"You're verified, **{command.User.Username}**! 🎉", ephemeral: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verifying Discord user {DiscordUserId} failed", command.User.Id);
            await command.FollowupAsync("❌ Verification failed. Please try again later.", ephemeral: true);
        }
    }
}
