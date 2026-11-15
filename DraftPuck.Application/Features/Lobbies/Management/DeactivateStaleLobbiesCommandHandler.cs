using DraftPuck.Application.Features.Games;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class DeactivateStaleLobbiesCommandHandler(IDbContext dbContext, IMediator mediator, IOptions<ApplicationOptions> appConfig, ILogger<DeactivateStaleLobbiesCommandHandler> logger) : IRequestHandler<DeactivateStaleLobbiesCommand>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(DeactivateStaleLobbiesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cutoffAdjustedNow = GameProcessingHelpers.AdjustDateTimeForCutoff(_appConfig.CurrentTimeUtc);
            var cutoffAdjustedToday = cutoffAdjustedNow.Date;
            var cutoffBoundaryUtc = cutoffAdjustedToday.AddHours(GameProcessingHelpers.CutoffHours);

            // Only lobbies still active: ones already ended aren't touched (or announced) again.
            var lobbiesToDeactivate = await dbContext.Lobbies
                .Where(l => l.IsActive && l.Created < cutoffBoundaryUtc)
                .ToListAsync(cancellationToken);

            if (lobbiesToDeactivate.Count == 0) return;

            foreach (var lobby in lobbiesToDeactivate)
                lobby.IsActive = false;

            await dbContext.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new LobbiesEndedNotification([.. lobbiesToDeactivate.Select(l => l.Id)]), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deactivating stale lobbies.");
        }
    }
}
