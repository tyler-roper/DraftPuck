using Discord.WebSocket;
using DraftPuck.DiscordBot.Interfaces;

namespace DraftPuck.DiscordBot.Services;

public class DiscordEventHandler(IInternalApiClient apiClient)
{
    public async Task OnUserJoinedAsync(SocketGuildUser user, CancellationToken ct)
    {
        await apiClient.SendDiscordServerJoinedNotification(user.Id.ToString(), ct);
    }

    public async Task OnMessageReceivedAsync(SocketMessage message, CancellationToken ct)
    {
        Console.WriteLine("Message received...");

        if (message.Author.IsBot) return;

        if (message.Content.Trim().Equals("verify", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Console.WriteLine("Verifying...");
                await message.Channel.SendMessageAsync($"Verifying Discord user {message.Author.Username}...");
                await apiClient.SendDiscordServerJoinedNotification(message.Author.Id.ToString(), ct);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }

    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName == "verify")
        {
            await command.DeferAsync(ephemeral: true);

            try
            {
                await apiClient.SendDiscordServerJoinedNotification(
                    command.User.Id.ToString(), CancellationToken.None);

                await command.FollowupAsync(
                    $"You're verified, **{command.User.Username}**! 🎉",
                    ephemeral: true
                );
            }
            catch (Exception ex)
            {
                await command.FollowupAsync(
                    "❌ Verification failed. Please try again later.",
                    ephemeral: true
                );

                Console.WriteLine($"Slash command error: {ex}");
            }
        }
    }
}
