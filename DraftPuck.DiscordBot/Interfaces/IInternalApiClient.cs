namespace DraftPuck.DiscordBot.Interfaces;

public interface IInternalApiClient
{
    Task SendDiscordServerJoinedNotification(string userId, CancellationToken ct);
}
