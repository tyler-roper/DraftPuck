namespace DraftPuck.Application.Features.Achievements;

public class LobbyCreatedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<LobbyCreatedNotification>
{
    public async Task Handle(LobbyCreatedNotification notification, CancellationToken cancellationToken)
    {
        var creatorUserId = notification.Data.Lobby.CreatedBy;
        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = creatorUserId,
            TriggerType = AchievementTriggerType.LobbyCreated,
            QueuedTimeUtc = DateTime.UtcNow
        });
    }
}