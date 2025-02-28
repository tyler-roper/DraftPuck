using DraftPuck.Infrastructure.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Core.Services;

public class GameCheckerHostedService : IHostedService
{
    private Timer? _timer;
    private readonly IServiceProvider _services;
    private readonly ILogger<GameCheckerHostedService> _logger;

    public GameCheckerHostedService(IServiceProvider services, ILogger<GameCheckerHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting GameCheckerHostedService...");

        async void action()
        {
            await CheckGames(cancellationToken);

            _timer = new Timer(
                async _ => await CheckGames(cancellationToken),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(ApplicationOptions.GameCheckFrequencyInSeconds)
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

    private async Task CheckGames(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var gameService = scope.ServiceProvider.GetService<IGameService>();
        if (gameService == null)
        {
            return;
        }

        await gameService.CheckGamesAsync();
    }
}
