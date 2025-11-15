namespace DraftPuck.Application.Features.Achievements;

public class DrinkAssignedAchievementHandler(INhlQueueService queueService) : INotificationHandler<DrinkAssignedNotification>
{
    public async Task Handle(DrinkAssignedNotification notification, CancellationToken ct)
    {
        var sender = notification.Data.Sender;
        var recipient = notification.Data.Recipient;

        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = sender.UserId,
            TriggerType = AchievementTriggerType.DrinkAssigned,
            QueuedTimeUtc = DateTime.UtcNow
        });

        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = recipient.UserId,
            TriggerType = AchievementTriggerType.DrinkAssigned,
            QueuedTimeUtc = DateTime.UtcNow
        });
    }
}