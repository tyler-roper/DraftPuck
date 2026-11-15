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

        // Notification settings are per lobby: a user hears about the game if any of their lobbies with it has picking
        // notifications on, and the notification opens the first such lobby.
        var lobbyByUser = lobbies
            .SelectMany(l => l.LobbyMembers
                .Where(m => !m.IsBot && !m.IsRemoved && m.PickingStartedNotificationPreference != NotificationPreference.None)
                .Select(m => (m.UserId, Lobby: l)))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().Lobby);

        var userIds = lobbyByUser.Keys.ToList();
        var usersWithPushNotificationsEnabled = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmRegistrationToken))
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        foreach (var (userId, userLobby) in lobbyByUser)
        {
            if (!usersWithPushNotificationsEnabled.TryGetValue(userId, out var user))
                continue;

            if (await gameCache.HasUserBeenNotifiedForGameAsync(userId, game.Id))
                continue;

            // Games starting together are processed in parallel; claim the user's 15-minute window atomically
            // so they get one "one or more games" notification rather than one per game.
            if (!await gameCache.TryClaimUserPicksNotificationAsync(userId))
                continue;

            var sent = await pushService.SendLobbyEventNotification(
                userLobby.JoinCode,
                "Time to pick players!",
                "One or more games is about to start! Make your picks now.",
                user.FcmRegistrationToken!,
                ct: ct
            );

            if (!sent)
            {
                logger.LogWarning("Failed to send picks-ready notification to User {userId} for game {gameId} via lobby {lobbyCode}.",
                    userId, game.Id, userLobby.JoinCode);
                await gameCache.ReleaseUserPicksNotificationAsync(userId);
                continue;
            }

            await gameCache.MarkUserNotifiedForGameAsync(userId, game.Id, _appConfig.CurrentTimeUtc, game.DateTime);

            logger.LogInformation("Sent picks-ready notification to User {userId} for game {gameId} via lobby {lobbyCode}.",
                userId, game.Id, userLobby.JoinCode);
        }
    }
}

