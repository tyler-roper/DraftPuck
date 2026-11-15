namespace DraftPuck.Application.Features.Achievements;

/// <summary>Checks who gave and who received the drink, and the host when one of their lobby's bots gave it.</summary>
public class DrinkAssignedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<DrinkAssignedNotification>
{
    public async Task Handle(DrinkAssignedNotification notification, CancellationToken ct)
    {
        var (lobby, sender, recipient) = (notification.Data.Lobby, notification.Data.Sender, notification.Data.Recipient);

        var userIds = new List<Guid>();
        if (!sender.IsBot) userIds.Add(sender.UserId);
        if (!recipient.IsBot) userIds.Add(recipient.UserId);
        if (sender.IsBot) userIds.Add(lobby.CreatedBy); // Puppet Master: the host's bots handing out drinks

        foreach (var userId in userIds.Distinct())
        {
            await queueService.SendMessageAsync(new CheckAchievementsMessage
            {
                UserId = userId,
                TriggerType = AchievementTriggerType.DrinkAssigned,
                QueuedTimeUtc = DateTime.UtcNow
            }, ct: ct);
        }
    }
}
