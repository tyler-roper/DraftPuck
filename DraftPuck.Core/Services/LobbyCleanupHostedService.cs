using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Core.Services;

public class LobbyCleanupHostedService : IHostedService
{
    private Timer? _timer;
    private readonly IServiceProvider _services;
    private readonly ILogger<LobbyCleanupHostedService> _logger;

    public LobbyCleanupHostedService(IServiceProvider services, ILogger<LobbyCleanupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting LobbyCleanupHostedService...");

        TimeSpan interval = TimeSpan.FromHours(24);
        DateTime nextMidnight = DateTime.Today.AddDays(1);
        DateTime curTime = DateTime.Now;
        TimeSpan firstInterval = nextMidnight.Subtract(curTime);

        async void action()
        {
            await Task.Delay(firstInterval, cancellationToken);

            _timer = new Timer(
                async _ => await DeleteOldLobbies(cancellationToken),
                null,
                TimeSpan.Zero,
                interval
            );
        }

        _ = Task.Run(action, cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task DeleteOldLobbies(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _services.CreateScope();
        ILobbyService? lobbyService = scope.ServiceProvider.GetService<ILobbyService>();
        if (lobbyService == null)
        {
            return;
        }

        await lobbyService.DeleteOldLobbies();
    }
}
