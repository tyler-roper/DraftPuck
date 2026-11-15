using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Achievements;

public class NameChangedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<UserNameChangedNotification>
{
    public async Task Handle(UserNameChangedNotification notification, CancellationToken ct)
    {
        var member = notification.Data.Member;
        if (member.IsBot) return;

        await queueService.SendMessageAsync(new CheckAchievementsMessage
        {
            UserId = member.UserId,
            TriggerType = AchievementTriggerType.NameChanged,
            QueuedTimeUtc = DateTime.UtcNow
        }, ct: ct);
    }
}
