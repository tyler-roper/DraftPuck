using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.PushNotifications;

public class MessageSentPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<MessageSentNotification>
{
    public async Task Handle(MessageSentNotification notification, CancellationToken ct)
    {
        var (lobby, sender, message) = (notification.Data.Lobby, notification.Data.Sender, notification.Data.Message);

        var userIds = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && lm.UserId != sender.UserId && !lm.IsRemoved)
            .Select(lm => lm.UserId);

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmRegistrationToken != null)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var userName = lobby.LobbyMembers.Single(lm => lm.UserId == user.Id).Name;
            var isMentioned = message.Message.Contains($"@{userName}", StringComparison.OrdinalIgnoreCase);

            if (!isMentioned && user.ChatNotificationPreference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "chatMessage" }, { "isRelevant", "false" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, sender.Name, message.Message, user.FcmRegistrationToken!, data);
            }
            else if (isMentioned && user.ChatNotificationPreference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "chatMessage" }, { "isRelevant", "true" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, $"{sender.Name} mentioned you.", message.Message, user.FcmRegistrationToken!, data);
            }
        });
    }
}