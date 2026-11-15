using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;

namespace DraftPuck.DiscordBot.Services;

public class DiscordEventHandler(IInternalApiClient apiClient, ILogger<DiscordEventHandler> logger)
{
    public async Task OnUserJoinedAsync(SocketGuildUser user, CancellationToken ct)
    {
        await apiClient.SendDiscordServerJoinedNotification(user.Id.ToString(), ct);
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
