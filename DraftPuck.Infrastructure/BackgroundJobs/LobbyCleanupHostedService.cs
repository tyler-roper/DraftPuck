using DraftPuck.Application.Features.Lobbies;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Infrastructure.BackgroundJobs;

public class LobbyCleanupHostedService(IServiceProvider serviceProvider, ILogger<LobbyCleanupHostedService> logger) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting LobbyCleanupHostedService...");

        var nowUtc = DateTime.UtcNow;
        var nextMidnightUtc = nowUtc.Date.AddDays(1);
        var firstInterval = nextMidnightUtc - nowUtc;

        logger.LogInformation("Lobby cleanup scheduled to run first at {NextRunTimeUtc} UTC (in {FirstInterval}). Will run every 24 hours thereafter.", nextMidnightUtc, firstInterval);

        _timer = new Timer(
            DoWork,
            null,
            firstInterval,
            TimeSpan.FromHours(24)
        );

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        logger.LogInformation("LobbyCleanupHostedService is running...");

        try
        {
            using var scope = serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new DeactivateStaleLobbiesCommand());

            logger.LogInformation("LobbyCleanupHostedService finished work.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during scheduled lobby cleanup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping LobbyCleanupHostedService.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}