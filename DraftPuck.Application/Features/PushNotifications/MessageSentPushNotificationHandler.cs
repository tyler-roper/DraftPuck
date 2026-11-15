using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.PushNotifications;

public class MessageSentPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<MessageSentNotification>
{
    public async Task Handle(MessageSentNotification notification, CancellationToken ct)
    {
        var (lobby, sender, message) = (notification.Data.Lobby, notification.Data.Sender, notification.Data.Message);

        // Each member's settings for this lobby decide whether they hear about it.
        var members = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && lm.UserId != sender.UserId && !lm.IsRemoved)
            .ToDictionary(lm => lm.UserId);

        var userIds = members.Keys.ToList();
        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmRegistrationToken != null)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var member = members[user.Id];
            var preference = member.ChatNotificationPreference;
            var isMentioned = message.Message.Contains($"@{member.Name}", StringComparison.OrdinalIgnoreCase);

            if (!isMentioned && preference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "chatMessage" }, { "isRelevant", "false" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, sender.Name, message.Message, user.FcmRegistrationToken!, data);
            }
            else if (isMentioned && preference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "chatMessage" }, { "isRelevant", "true" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, $"{sender.Name} mentioned you.", message.Message, user.FcmRegistrationToken!, data);
            }
        });
    }
}
