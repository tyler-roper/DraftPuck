using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.PushNotifications;

public class DrinkAssignedPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<DrinkAssignedNotification>
{
    public async Task Handle(DrinkAssignedNotification notification, CancellationToken ct)
    {
        var (lobby, sender, recipient) = (notification.Data.Lobby, notification.Data.Sender, notification.Data.Recipient);

        var userIds = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && lm.UserId != sender.UserId && !lm.IsRemoved)
            .Select(lm => lm.UserId);

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmRegistrationToken != null)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var isRecipient = user.Id == recipient.UserId;
            var text = $"Drink assigned to {recipient.Name} by {sender.Name}";

            if (!isRecipient && user.DrinkReceivedNotificationPreference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAssigned" }, { "isRelevant", "false" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, "Drink Assigned", text, user.FcmRegistrationToken!, data);
            }
            else if (isRecipient && user.DrinkReceivedNotificationPreference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAssigned" }, { "isRelevant", "true" } };
                await firebaseService.SendPushNotification(lobby.JoinCode, "🍺 DRINK 🍺", text, user.FcmRegistrationToken!, data);
            }
        });
    }
}