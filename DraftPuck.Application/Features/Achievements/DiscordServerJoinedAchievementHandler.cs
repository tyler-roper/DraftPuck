using DraftPuck.Shared.Discord;

namespace DraftPuck.Application.Features.Achievements;

public class DiscordServerJoinedAchievementHandler(IAchievementQueueService queueService, IDbContext dbContext) : INotificationHandler<DiscordServerJoinedNotification>
{
    public async Task Handle(DiscordServerJoinedNotification notification, CancellationToken cancellationToken)
    {
        var discordUserId = notification.DiscordUserId;
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId, cancellationToken);

        if (user == null)
            return;

        await queueService.SendMessageAsync(new CheckAchievementsMessage()
        {
            UserId = user.Id,
            TriggerType = AchievementTriggerType.DiscordServerJoined,
            QueuedTimeUtc = DateTime.UtcNow
        }, ct: cancellationToken);
    }
}