using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.PushNotifications;

public class DrinkAwardedPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<DrinkAwardedNotification>
{
    public async Task Handle(DrinkAwardedNotification notification, CancellationToken ct)
    {
        var (lobby, sender) = (notification.Data.Lobby, notification.Data.Member);

        // Each member's settings for this lobby decide whether they hear about it.
        var members = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && !lm.IsRemoved)
            .ToDictionary(lm => lm.UserId);

        var userIds = members.Keys.ToList();
        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmRegistrationToken != null)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var preference = members[user.Id].DrinkAwardedNotificationPreference;
            var isSender = user.Id == sender.UserId;
            var text = $"Drink awarded to {sender.Name}!";

            if (!isSender && preference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAwarded" }, { "isRelevant", "false" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", text, user.FcmRegistrationToken!, data);
            }
            // "Notify me if it's mine": your own player scored, so you have a drink to give out.
            else if (isSender && preference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAwarded" }, { "isRelevant", "true" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "Drink Awarded", "Your player scored - give out a drink!", user.FcmRegistrationToken!, data);
            }
        });
    }
}
