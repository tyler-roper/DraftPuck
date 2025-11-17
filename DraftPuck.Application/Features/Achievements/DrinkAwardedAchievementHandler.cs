namespace DraftPuck.Application.Features.Achievements;

public class DrinkAwardedAchievementHandler(INhlQueueService queueService) : INotificationHandler<DrinkAwardedNotification>
{
    public async Task Handle(DrinkAwardedNotification notification, CancellationToken ct)
    {
        var member = notification.Data.Member;
        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = member.UserId,
            TriggerType = AchievementTriggerType.DrinkAwarded,
            QueuedTimeUtc = DateTime.UtcNow
        });
    }
}