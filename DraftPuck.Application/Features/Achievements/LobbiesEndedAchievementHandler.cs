using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Achievements;

/// <summary>When lobbies end, checks their human members (The Cup is decided once a lobby is over).</summary>
public class LobbiesEndedAchievementHandler(IAchievementQueueService queueService, IDbContext dbContext) : INotificationHandler<LobbiesEndedNotification>
{
    public async Task Handle(LobbiesEndedNotification notification, CancellationToken ct)
    {
        if (notification.LobbyIds.Count == 0) return;

        var userIds = await dbContext.LobbyMembers
            .Where(lm => notification.LobbyIds.Contains(lm.LobbyId) && !lm.IsBot && !lm.IsRemoved)
            .Select(lm => lm.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            await queueService.SendMessageAsync(new CheckAchievementsMessage
            {
                UserId = userId,
                TriggerType = AchievementTriggerType.LobbyEnded,
                QueuedTimeUtc = DateTime.UtcNow
            }, ct: ct);
        }
    }
}
