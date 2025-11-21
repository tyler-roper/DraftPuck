namespace DraftPuck.Application.Features.Achievements;

public class LobbyJoinedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<UserJoinedLobbyNotification>
{
    public async Task Handle(UserJoinedLobbyNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Data.Lobby.CreatedBy == notification.Data.Member.UserId) return;
        var userId = notification.Data.Member.UserId;
        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = userId,
            TriggerType = AchievementTriggerType.LobbyJoined,
            QueuedTimeUtc = DateTime.UtcNow
        });
    }
}