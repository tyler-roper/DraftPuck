using DraftPuck.Application.Features.Games;
using DraftPuck.Shared.Firebase;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.PushNotifications;
public class NotifyUsersPicksReadyNotificationHandler(IDbContext dbContext, IGameCache gameCache, IPushNotificationService pushService, IOptions<ApplicationOptions> appConfig, ILogger<NotifyUsersPicksReadyNotificationHandler> logger) : INotificationHandler<PicksReadyNotification>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;
    public async Task Handle(PicksReadyNotification notification, CancellationToken ct)
    {
        var game = notification.Game;

        var lobbies = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
            .Where(l => l.GameIds.Contains(game.Id))
            .ToListAsync(ct);

        var userIds = lobbies
            .SelectMany(l => l.LobbyMembers.Select(m => m.UserId))
            .Distinct()
            .ToList();

        var usersWithPushNotificationsEnabled = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmRegistrationToken))
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        foreach (var userId in userIds)
        {
            if (!usersWithPushNotificationsEnabled.TryGetValue(userId, out var user))
                continue;

            if (await gameCache.HasUserBeenNotifiedForGameAsync(userId, game.Id))
                continue;

            if (await gameCache.HasUserBeenNotifiedRecentlyAsync(userId))
                continue;

            var userLobby = lobbies.FirstOrDefault(l => l.LobbyMembers.Any(m => m.UserId == userId));
            if (userLobby == null)
                continue;

            await pushService.SendLobbyEventNotification(
                userLobby.JoinCode,
                "Time to pick players!",
                "One or more games is about to start! Make your picks now.",
                user.FcmRegistrationToken!
            );

            await gameCache.MarkUserAsNotifiedAsync(userId);
            await gameCache.MarkUserNotifiedForGameAsync(userId, game.Id, _appConfig.CurrentTimeUtc, game.DateTime);

            logger.LogInformation("Sent picks-ready notification to User {userId} for game {gameId} via lobby {lobbyCode}.",
                userId, game.Id, userLobby.JoinCode);
        }
    }
}

