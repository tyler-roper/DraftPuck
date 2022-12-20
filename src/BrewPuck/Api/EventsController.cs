using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace BrewPuck.Api
{
    public class EventsController : BrewPuckApiControllerBase
    {
        private readonly IEventService _eventService;
        private readonly INotificationService _notificationService;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public EventsController(IEventService eventService, INotificationService notificationService)
        {
            _eventService = eventService;
            _notificationService = notificationService;
        }

        [Produces("text/event-stream")]
        [HttpGet]
        public async Task ListenForNotifications(CancellationToken cancellationToken)
        {
            SetServerSentEventHeaders();
            await Response.WriteAsync($"event:connected\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            async void OnNotification(object? sender, NotificationArgs eventArgs)
            {
                var json = JsonSerializer.Serialize(eventArgs.Notification, _jsonSerializerOptions);
                await Response.WriteAsync("retry:10000\n", cancellationToken);
                await Response.WriteAsync($"event:notification\n", cancellationToken);
                await Response.WriteAsync($"data:{json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            async void KeepAlive(object? sender, EventArgs eventArgs)
            {
                await Response.WriteAsync("event:keep-alive\n", cancellationToken);
                await Response.WriteAsync("data:keep-alive\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            _eventService.NotificationEvent += OnNotification;
            _eventService.KeepAlive += KeepAlive;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                    await Task.Delay(1000, cancellationToken);
            }
            finally
            {
                _eventService.NotificationEvent -= OnNotification;
                _eventService.KeepAlive -= KeepAlive;
            }
        }

        [HttpPost("notifications")]
        [AllowAnonymous]
        public async Task Broadcast(string message)
        {
            _notificationService.SendMessage(message);
        }

        private void SetServerSentEventHeaders()
        {
            Response.StatusCode = 200;
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
        }
    }
}
