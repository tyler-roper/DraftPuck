using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Achievements;

/// <summary>A drink that was already given out lost its goal: checks the giver (False Alarm) and who drank it (Waved Off).</summary>
public class DrinkInvalidatedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<DrinkInvalidatedNotification>
{
    public async Task Handle(DrinkInvalidatedNotification notification, CancellationToken ct)
    {
        var (sender, recipient) = (notification.Data.Sender, notification.Data.Recipient);

        foreach (var member in new[] { sender, recipient }.Where(m => !m.IsBot).DistinctBy(m => m.UserId))
        {
            await queueService.SendMessageAsync(new CheckAchievementsMessage
            {
                UserId = member.UserId,
                TriggerType = AchievementTriggerType.DrinkInvalidated,
                QueuedTimeUtc = DateTime.UtcNow
            }, ct: ct);
        }
    }
}
