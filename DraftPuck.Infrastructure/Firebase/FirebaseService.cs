using DraftPuck.Shared.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;

namespace DraftPuck.Infrastructure.Firebase;
public class FirebaseService : IFirebaseService
{

    private readonly FirebaseApp _app;

    public FirebaseService(FirebaseApp app)
    {
        _app = app;
    }

    public async Task SendPushNotification(string message, string token)
    {
        var firebaseMessage = new Message()
        {
            Token = token,
            Notification = new Notification()
            {
                Title = "Here's a test!",
                Body = message
            },
            Webpush = new WebpushConfig()
            {
                FcmOptions = new() { Link = "https://draftpuck.com/lobby/" }
            }
        };

        var response = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage);
    }
}
