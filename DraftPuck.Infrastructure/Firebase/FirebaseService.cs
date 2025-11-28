using DraftPuck.Shared.Firebase;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;
public class FirebaseService(IOptions<ApplicationOptions> appConfig, FirebaseApp firebaseApp) : IPushNotificationService
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;
    private static class PushNotificationTypes
    {
        public static readonly string LOBBY_EVENT = "LobbyEvent";
        public static readonly string ACHIEVEMENT = "Achievement";
    }

    public async Task SendAchievementNotification(string token, AchievementEntity achievement, CancellationToken ct)
    {
        var firebaseMessage = new Message()
        {
            Token = token,
            Webpush = new WebpushConfig()
            {
                Notification = new WebpushNotification()
                {
                    Title = $"Achievement earned: {achievement.FriendlyName}",
                    Body = $"{achievement.Description}",
                    Icon = $"{_appConfig.BasePath}/img/icons/icon-192.png",
                    Badge = $"{_appConfig.BasePath}/img/icons/badge.png"
                }
            },
            Data = new Dictionary<string,string> { { "type", PushNotificationTypes.ACHIEVEMENT } }
        };

        try
        {
            var firebaseMessaging = FirebaseMessaging.GetMessaging(firebaseApp);
            await firebaseMessaging.SendAsync(firebaseMessage, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task SendLobbyEventNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null)
    {
        var firebaseMessage = new Message()
        {
            Token = token,
            Webpush = new WebpushConfig()
            {
                Notification = new WebpushNotification()
                {
                    Title = title,
                    Body = message,
                    Icon = $"{_appConfig.BasePath}img/icons/icon-192.png",
                    Badge = $"{_appConfig.BasePath}img/icons/badge.png"
                },
                FcmOptions = new WebpushFcmOptions()
                {
                    Link = $"{_appConfig.BasePath}lobby/{lobbyCode}"
                }
            },
            Data = new Dictionary<string, string> { { "type", PushNotificationTypes.LOBBY_EVENT } }
                .Concat(data ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .ToDictionary(k => k.Key, v => v.Value)
        };

        try
        {
            var firebaseMessaging = FirebaseMessaging.GetMessaging(firebaseApp);
            await firebaseMessaging.SendAsync(firebaseMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
