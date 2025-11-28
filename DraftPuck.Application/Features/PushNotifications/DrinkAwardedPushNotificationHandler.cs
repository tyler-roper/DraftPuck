using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.PushNotifications;

public class DrinkAwardedPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<DrinkAwardedNotification>
{
    public async Task Handle(DrinkAwardedNotification notification, CancellationToken ct)
    {
        var (lobby, sender) = (notification.Data.Lobby, notification.Data.Member);

        var userIds = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && !lm.IsRemoved)
            .Select(lm => lm.UserId);

        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmRegistrationToken != null)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var isSender = user.Id == sender.UserId;
            var text = $"Drink awarded to {sender.Name}!";

            if (!isSender && user.DrinkAwardedNotificationPreference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAwarded" }, { "isRelevant", "false" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", text, user.FcmRegistrationToken!, data);
            }
            else if (isSender && user.DrinkReceivedNotificationPreference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAwarded" }, { "isRelevant", "true" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", "Your player scored - give out a drink!", user.FcmRegistrationToken!, data);
            }
        });
    }
}