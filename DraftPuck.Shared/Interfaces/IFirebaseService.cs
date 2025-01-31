namespace DraftPuck.Shared.Interfaces;
public interface IFirebaseService
{
    public Task SendPushNotification(string lobbyCode, string title, string message, string token);
}
