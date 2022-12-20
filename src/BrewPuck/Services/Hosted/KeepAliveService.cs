namespace BrewPuck.Services.Hosted
{
    public class KeepAliveService : IHostedService
    {
        private Timer? _timer;
        private readonly IEventService _eventService;

        public KeepAliveService(IEventService eventService)
        {
            _eventService = eventService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromSeconds(15);

            void action()
            {
                _timer = new Timer(
                    _ => SendKeepAliveMessages(cancellationToken),
                    null,
                    TimeSpan.Zero,
                    interval
                );
            }

            Task.Run(action, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        private void SendKeepAliveMessages(CancellationToken cancellationToken)
        {
            _eventService.SendKeepAliveMessages();
        }
    }
}
