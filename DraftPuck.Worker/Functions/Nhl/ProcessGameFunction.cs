using DraftPuck.Application.Features.Games;
using DraftPuck.Shared.Games;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Worker.Functions.Nhl;

public class ProcessGameFunction(IMediator mediator, ILogger<ProcessGameFunction> logger)
{
    [Function(nameof(ProcessGameFunction))]
    public async Task ProcessGame([QueueTrigger("nhl-queue")] ProcessGameMessage message)
    {
        try
        {
            logger.LogInformation("ProcessGameFunction triggered for game {GameId}", message.GameId);
            await mediator.Send(new ProcessGameCommand(message.GameId, message.IsInitialPopulation));
            logger.LogInformation("ProcessGameCommand for {GameId} dispatched", message.GameId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error processing game {message.GameId}");
            throw;
        }
    }
}
