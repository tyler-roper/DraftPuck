using DraftPuck.Shared.Firebase;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;
public class FirebaseService(IOptions<ApplicationOptions> appConfig, FirebaseApp firebaseApp) : IPushNotificationService
{

    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task SendPushNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null)
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
                    Icon = $"{_appConfig.BasePath}/img/icons/icon-192.png",
                    Badge = $"{_appConfig.BasePath}/img/icons/badge.png"
                },
                FcmOptions = new WebpushFcmOptions()
                {
                    Link = $"{_appConfig.BasePath}/lobby/{lobbyCode}"
                }
            },
            Data = data
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
