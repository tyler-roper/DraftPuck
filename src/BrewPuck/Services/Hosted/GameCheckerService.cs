using BrewPuck.Models.NHL;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrewPuck.Services.Hosted
{
    public class GameCheckerService : IHostedService
    {
        private Timer? _timer;
        private readonly IGameService _gameService;

        public GameCheckerService(IGameService gameService)
        {
            _gameService = gameService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(10);

            void action()
            {
                _timer = new Timer(
                    async _ => await _gameService.GetSchedule(cancellationToken),
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
    }
}
