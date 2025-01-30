namespace DraftPuck.Shared.Interfaces;
public interface IFirebaseService
{
    public Task SendPushNotification(string message, string token);
}
