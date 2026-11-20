using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;

namespace DraftPuck.DiscordBot.Services;

public class DiscordEventHandler(IInternalApiClient apiClient, ILogger<DiscordEventHandler> logger)
{
    /// <summary>
    /// Verifies a member as they join, so they don't have to run /verify. Joiners without a DraftPuck account are
    /// ignored by the web app. If it can't be reached, the member can still /verify later.
    /// </summary>
    public async Task OnUserJoinedAsync(SocketGuildUser user, CancellationToken ct)
    {
        try
        {
            await apiClient.SendDiscordServerJoinedNotification(user.Id.ToString(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Verifying Discord user {DiscordUserId} on joining failed; they can run /verify.", user.Id);
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
