namespace BrewPuck.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEventService _eventService;

        public NotificationService(IEventService eventService)
        {
            _eventService = eventService;
        }

        public void SendMessage(string message)
        {
            _eventService.Notify(new NotificationModel() { TestMessage = message });
        }
    }
}
