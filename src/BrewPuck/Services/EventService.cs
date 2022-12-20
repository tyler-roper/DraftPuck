namespace BrewPuck.Services
{
    public class EventService : IEventService
    {
        public event EventHandler<NotificationArgs>? NotificationEvent;
        public event EventHandler? KeepAlive;

        public void Notify(NotificationModel notification)
        {
            NotificationEvent?.Invoke(this, new NotificationArgs(notification));
        }

        public void SendKeepAliveMessages()
        {
            KeepAlive?.Invoke(this, EventArgs.Empty);
        }
    }

    public class NotificationArgs : EventArgs
    {
        public NotificationModel Notification { get; }

        public NotificationArgs(NotificationModel notification)
        {
            Notification = notification;
        }
    }

    public class NotificationModel
    {
        public string TestMessage { get; set; }
    }
}
