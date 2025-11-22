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
            Notification = new Notification()
            {
                Title = title,
                Body = message,
                ImageUrl = $"{_appConfig.BasePath}/img/icons/icon-192.png"
            },
            Android = new()
            {
                Notification = new()
                {
                    Icon = $"{_appConfig.BasePath}/img/icons/badge.png",
                    ImageUrl = $"{_appConfig.BasePath}/img/icons/icon-192.png",
                    Color = "#ffce00"
                }
            }
        };

        if (_appConfig.BasePath.StartsWith("https://"))
        {
            firebaseMessage.Webpush = new WebpushConfig() { FcmOptions = new() { Link = $"{_appConfig.BasePath}/lobby/{lobbyCode}" } };
        }

        if (data != null)
        {
            firebaseMessage.Data = data;
        }

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
