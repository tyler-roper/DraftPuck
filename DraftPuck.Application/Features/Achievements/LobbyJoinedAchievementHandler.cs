using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Achievements;

/// <summary>Checks the joiner (lobbies joined) and the host (how big their lobby has grown).</summary>
public class LobbyJoinedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<UserJoinedLobbyNotification>
{
    public async Task Handle(UserJoinedLobbyNotification notification, CancellationToken cancellationToken)
    {
        var (lobby, member) = (notification.Data.Lobby, notification.Data.Member);

        // The host "joins" when they create the lobby; that's LobbyCreated.
        if (lobby.CreatedBy == member.UserId) return;

        // A bot joining earns nothing itself, but grows the host's lobby (Bot Army).
        if (member.IsBot)
        {
            await queueService.SendMessageAsync(new CheckAchievementsMessage
            {
                UserId = lobby.CreatedBy,
                TriggerType = AchievementTriggerType.HostedLobbyJoined,
                QueuedTimeUtc = DateTime.UtcNow
            }, ct: cancellationToken);
            return;
        }

        await queueService.SendMessageAsync(new CheckAchievementsMessage
        {
            UserId = member.UserId,
            TriggerType = AchievementTriggerType.LobbyJoined,
            QueuedTimeUtc = DateTime.UtcNow
        }, ct: cancellationToken);

        await queueService.SendMessageAsync(new CheckAchievementsMessage
        {
            UserId = lobby.CreatedBy,
            TriggerType = AchievementTriggerType.HostedLobbyJoined,
            QueuedTimeUtc = DateTime.UtcNow
        }, ct: cancellationToken);
    }
}
