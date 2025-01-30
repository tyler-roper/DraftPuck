namespace DraftPuck.Shared.Interfaces;
public interface IFirebaseService
{
    public Task SendTestMessage(string message, string token);
}
