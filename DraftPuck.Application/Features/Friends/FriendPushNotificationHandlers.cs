namespace DraftPuck.Application.Features.Friends;

/// <summary>
/// Pushes about friends, under each user's Friends setting. A failed push mustn't fail the request that caused it, so
/// each handler logs its own errors.
/// </summary>
public class FriendPushNotificationHandlers(IDbContext dbContext, IPushNotificationService pushService, ILogger<FriendPushNotificationHandlers> logger)
    : INotificationHandler<FriendRequestedNotification>,
      INotificationHandler<FriendRequestAcceptedNotification>,
      INotificationHandler<LobbyInvitesSentNotification>
{
    /// <summary>The page with the user's friends and requests.</summary>
    public const string FriendsPath = "account/friends";

    public Task Handle(FriendRequestedNotification notification, CancellationToken ct) =>
        SafelyAsync(async () =>
        {
            var tokens = await TokensAsync([notification.Addressee.Id], ct);
            await pushService.SendFriendNotification("Friend request", $"{notification.Requester.Nickname} wants to be friends.", FriendsPath, tokens, ct);
        });

    public Task Handle(FriendRequestAcceptedNotification notification, CancellationToken ct) =>
        SafelyAsync(async () =>
        {
            var tokens = await TokensAsync([notification.Requester.Id], ct);
            await pushService.SendFriendNotification("Friend request accepted", $"You and {notification.Addressee.Nickname} are now friends.", FriendsPath, tokens, ct);
        });

    public Task Handle(LobbyInvitesSentNotification notification, CancellationToken ct) =>
        SafelyAsync(async () =>
        {
            var (lobby, sender, recipientIds) = notification;
            var tokens = await TokensAsync(recipientIds, ct);
            var who = sender.User?.Nickname ?? sender.Name;
            await pushService.SendFriendNotification("Lobby invite", $"{who} invited you to their lobby. {GamesText(lobby)} Tap to join!", $"lobby/{lobby.JoinCode}", tokens, ct);
        });

    private static string GamesText(LobbyEntity lobby) => lobby.GameIds.Count == 1 ? "1 game tonight." : $"{lobby.GameIds.Count} games tonight.";

    /// <summary>The devices of those of these users who have friend pushes on.</summary>
    private async Task<IReadOnlyCollection<string>> TokensAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        await dbContext.UserDevices
            .Where(d => userIds.Contains(d.UserId) && d.User.IsActive && d.User.FriendNotificationPreference != NotificationPreference.None)
            .Select(d => d.Token)
            .ToListAsync(ct);

    private async Task SafelyAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't send a friend push notification.");
        }
    }
}
