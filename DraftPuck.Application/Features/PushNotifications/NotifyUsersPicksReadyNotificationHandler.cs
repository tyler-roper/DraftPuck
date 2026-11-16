using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies;
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
            .Include(u => u.Devices)
            .Where(u => userIds.Contains(u.Id) && u.Devices.Any())
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        var gamesByUser = await GamesOpeningWithAsync(game, userIds, ct);

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

            var games = gamesByUser.GetValueOrDefault(userId) ?? [game];
            var sent = await pushService.SendLobbyEventNotification(
                userLobby.JoinCode,
                PicksReadyMessage.Title,
                PicksReadyMessage.Body(games),
                user.PushTokens(),
                ct: ct
            );

            if (sent == 0)
            {
                logger.LogWarning("Failed to send picks-ready notification to User {userId} for game {gameId} via lobby {lobbyCode}.",
                    userId, game.Id, userLobby.JoinCode);
                await gameCache.ReleaseUserPicksNotificationAsync(userId);
                continue;
            }

            // Every game the notification named counts as notified, so none of them sends another later.
            foreach (var named in games)
                await gameCache.MarkUserNotifiedForGameAsync(userId, named.Id, _appConfig.CurrentTimeUtc, named.DateTime);

            logger.LogInformation("Sent picks-ready notification to User {userId} for game {gameId} via lobby {lobbyCode}.",
                userId, game.Id, userLobby.JoinCode);
        }
    }

    /// <summary>
    /// For each user, this game plus their other games whose picking opens with it or within the next 15 minutes (the
    /// window one notification covers), from lobbies where they have picking notifications on, not started yet.
    /// </summary>
    private async Task<Dictionary<Guid, List<GameDto>>> GamesOpeningWithAsync(GameDto game, List<Guid> userIds, CancellationToken ct)
    {
        var memberships = await dbContext.LobbyMembers
            .Where(m => userIds.Contains(m.UserId) && !m.IsRemoved && m.Lobby.IsActive
                && m.PickingStartedNotificationPreference != NotificationPreference.None)
            .Select(m => new { m.UserId, m.Lobby.GameIds })
            .ToListAsync(ct);

        var now = _appConfig.CurrentTimeUtc;
        var windowEnd = game.PickingOpensUtc + TimeSpan.FromMinutes(15);
        var openingTogether = new Dictionary<int, GameDto> { [game.Id] = game };
        foreach (var id in memberships.SelectMany(m => m.GameIds).Distinct().Where(id => id != game.Id))
        {
            var other = await gameCache.GetGameByIdAsync(id);
            if (other != null && other.PickingOpensUtc >= game.PickingOpensUtc && other.PickingOpensUtc <= windowEnd && other.DateTime > now)
                openingTogether[id] = other;
        }

        return memberships
            .GroupBy(m => m.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(m => m.GameIds).Append(game.Id).Distinct()
                    .Where(openingTogether.ContainsKey).Select(id => openingTogether[id]).ToList());
    }
}

