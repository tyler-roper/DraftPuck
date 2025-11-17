using DraftPuck.Application.Features.Games;
using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Lobbies;

public class DeactivateStaleLobbiesCommandHandler(IDbContext dbContext, IOptions<ApplicationOptions> appConfig, ILogger<DeactivateStaleLobbiesCommandHandler> logger) : IRequestHandler<DeactivateStaleLobbiesCommand>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(DeactivateStaleLobbiesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cutoffAdjustedNow = GameProcessingHelpers.AdjustDateTimeForCutoff(_appConfig.CurrentTimeUtc);
            var cutoffAdjustedToday = cutoffAdjustedNow.Date;
            var cutoffBoundaryUtc = cutoffAdjustedToday.AddHours(GameProcessingHelpers.CutoffHours);

            var lobbiesToDeactivate = await dbContext.Lobbies
                .Where(l => l.Created < cutoffBoundaryUtc)
                .ToListAsync();

            foreach (var lobby in lobbiesToDeactivate)
                lobby.IsActive = false;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deactivating stale lobbies.");
        }
    }
}