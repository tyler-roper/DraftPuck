using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.PushNotifications;

/// <summary>
/// Tells a member their pick was removed because the player left the lineup, so they can pick someone else.
/// Uses the member's "picking started" setting for this lobby: it's the setting for being told it's time to pick.
/// </summary>
public class PickScratchedPushNotificationHandler(IDbContext dbContext, IGameCache gameCache, IPushNotificationService pushService) : INotificationHandler<PickScratchedNotification>
{
    public async Task Handle(PickScratchedNotification notification, CancellationToken ct)
    {
        var (lobby, member, pick) = notification.Data;
        if (member.IsBot || member.IsRemoved || member.PickingStartedNotificationPreference == NotificationPreference.None) return;

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == member.UserId, ct);
        if (user?.FcmRegistrationToken == null)
            return;

        var data = new Dictionary<string, string> { { "lobbyEventType", nameof(LobbyEventType.PickScratched) }, { "isRelevant", "true" } };
        await pushService.SendLobbyEventNotification(lobby.JoinCode, "Pick scratched",
            $"{await PlayerNameAsync(pick)} isn't playing. Pick someone else!", user.FcmRegistrationToken, data, ct);
    }

    // The cache still holds the previous poll here, which had the player in its lineup.
    private async Task<string> PlayerNameAsync(LobbyMemberPickEntity pick)
    {
        var game = await gameCache.GetGameByIdAsync(pick.GameId);
        var player = game?.PlayerSummaries.Concat(game.FormerPlayers).FirstOrDefault(p => p.Id == pick.PlayerId);
        return player == null ? "One of your players" : $"{player.FirstName} {player.LastName}";
    }
}
