namespace DraftPuck.Shared.Firebase;
public interface IPushNotificationService
{
    Task SendPushNotification(string lobbyCode, string title, string message, string token, Dictionary<string, string>? data = null);
}
