namespace BrewPuck.Services.Interfaces
{
    public interface IEventService
    {
        event EventHandler<NotificationArgs>? NotificationEvent;
        event EventHandler? KeepAlive;
        void Notify(NotificationModel notification);
        void SendKeepAliveMessages();
    }
}
