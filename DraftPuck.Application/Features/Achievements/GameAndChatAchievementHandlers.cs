using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Achievements;

/// <summary>A game went final: checks everyone with a pick in it (Game Winner is decided once the game is over).</summary>
public class GameFinishedAchievementHandler(IAchievementQueueService queueService, IDbContext dbContext) : INotificationHandler<GameFinishedNotification>
{
    public async Task Handle(GameFinishedNotification notification, CancellationToken ct)
    {
        var gameId = notification.Game.Id;
        var userIds = await dbContext.LobbyMemberPicks
            .Where(p => p.GameId == gameId && p.IsActive && !p.LobbyMember.IsBot && !p.LobbyMember.IsRemoved)
            .Select(p => p.LobbyMember.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
            await queueService.SendMessageAsync(new CheckAchievementsMessage { UserId = userId, TriggerType = AchievementTriggerType.GameFinished, QueuedTimeUtc = DateTime.UtcNow }, ct: ct);
    }
}

public class PickScratchedAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<PickScratchedNotification>
{
    public async Task Handle(PickScratchedNotification notification, CancellationToken ct)
    {
        var member = notification.Data.Member;
        if (member.IsBot) return;

        await queueService.SendMessageAsync(new CheckAchievementsMessage { UserId = member.UserId, TriggerType = AchievementTriggerType.PickScratched, QueuedTimeUtc = DateTime.UtcNow }, ct: ct);
    }
}

public class MessageSentAchievementHandler(IAchievementQueueService queueService) : INotificationHandler<MessageSentNotification>
{
    public async Task Handle(MessageSentNotification notification, CancellationToken ct)
    {
        var sender = notification.Data.Sender;
        if (sender.IsBot) return;

        await queueService.SendMessageAsync(new CheckAchievementsMessage { UserId = sender.UserId, TriggerType = AchievementTriggerType.MessageSent, QueuedTimeUtc = DateTime.UtcNow }, ct: ct);
    }
}
