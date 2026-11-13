using DraftPuck.Shared.Firebase;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;
public class FirebaseService(IOptions<ApplicationOptions> appConfig, FirebaseApp firebaseApp, ILogger<FirebaseService> logger) : IPushNotificationService
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;
    private static class PushNotificationTypes
    {
        public static readonly string LOBBY_EVENT = "LobbyEvent";
        public static readonly string ACHIEVEMENT = "Achievement";
    }

    public Task<bool> SendAchievementNotification(string token, AchievementEntity achievement, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            { "type", PushNotificationTypes.ACHIEVEMENT },
            { "title", $"Achievement earned: {achievement.FriendlyName}" },
            { "body", achievement.Description ?? "" },
            { "icon", _appConfig.GetAbsoluteUrl("img/icons/icon-192.png") },
            { "badge", _appConfig.GetAbsoluteUrl("img/icons/badge.png") }
        };

        return Send(token, data, ct);
    }

    public Task<bool> SendLobbyEventNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var messageData = new Dictionary<string, string>
        {
            { "type", PushNotificationTypes.LOBBY_EVENT },
            { "title", title ?? "" },
            { "body", message ?? "" },
            { "icon", _appConfig.GetAbsoluteUrl("img/icons/icon-192.png") },
            { "badge", _appConfig.GetAbsoluteUrl("img/icons/badge.png") },
            { "link", _appConfig.GetAbsoluteUrl($"lobby/{lobbyCode}") }
        };

        if (data != null)
        {
            foreach (var kvp in data)
                messageData[kvp.Key] = kvp.Value ?? "";
        }

        return Send(token, messageData, ct);
    }

    private async Task<bool> Send(string token, Dictionary<string, string> data, CancellationToken ct)
    {
        // Data-only message: the service worker builds and shows the notification itself.
        // High urgency so delivery isn't deferred while the device is idle (e.g. Android doze).
        var firebaseMessage = new Message()
        {
            Token = token,
            Data = data,
            Webpush = new WebpushConfig()
            {
                Headers = new Dictionary<string, string> { { "Urgency", "high" } }
            }
        };

        try
        {
            var firebaseMessaging = FirebaseMessaging.GetMessaging(firebaseApp);
            await firebaseMessaging.SendAsync(firebaseMessage, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {type} push notification.", data.GetValueOrDefault("type"));
            return false;
        }
    }
}
