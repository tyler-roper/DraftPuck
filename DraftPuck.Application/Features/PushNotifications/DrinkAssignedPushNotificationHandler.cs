using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.PushNotifications;

public class DrinkAssignedPushNotificationHandler(IPushNotificationService firebaseService, IDbContext dbContext) : INotificationHandler<DrinkAssignedNotification>
{
    public async Task Handle(DrinkAssignedNotification notification, CancellationToken ct)
    {
        var (lobby, sender, recipient) = (notification.Data.Lobby, notification.Data.Sender, notification.Data.Recipient);
        var quote = notification.Data.Drink.Message is { } message ? $" “{message}”" : "";

        // Each member's settings for this lobby decide whether they hear about it.
        var members = lobby.LobbyMembers
            .Where(lm => !lm.IsBot && lm.UserId != sender.UserId && !lm.IsRemoved)
            .ToDictionary(lm => lm.UserId);

        var userIds = members.Keys.ToList();
        var users = await dbContext.Users
            .Include(u => u.Devices)
            .Where(u => userIds.Contains(u.Id) && u.Devices.Any())
            .ToListAsync(ct);

        await Parallel.ForEachAsync(users, ct, async (user, _) =>
        {
            var preference = members[user.Id].DrinkReceivedNotificationPreference;
            var isRecipient = user.Id == recipient.UserId;
            var text = $"Drink assigned to {recipient.Name} by {sender.Name}{quote}";

            if (!isRecipient && preference == NotificationPreference.All)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAssigned" }, { "isRelevant", "false" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "Drink Assigned", text, user.PushTokens(), data);
            }
            else if (isRecipient && preference != NotificationPreference.None)
            {
                var data = new Dictionary<string, string> { { "lobbyEventType", "DrinkAssigned" }, { "isRelevant", "true" } };
                await firebaseService.SendLobbyEventNotification(lobby.JoinCode, "🍺 DRINK 🍺", $"{sender.Name} gave you a drink!{quote}", user.PushTokens(), data);
            }
        });
    }
}
