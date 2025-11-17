using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Worker.Functions.Common;

public class ApplyCutoffFunction(IMediator mediator, ILogger<ApplyCutoffFunction> logger)
{
    private const string utc1001Am = "1 10 * * *";

    [Function("ApplyCutoffFunction")]
    public async Task ApplyCutoff([TimerTrigger(utc1001Am, RunOnStartup = true)] TimerInfo _)
    {
        logger.LogInformation("DeactivateStaleLobbiesFunction triggered.");
        await mediator.Send(new DeactivateStaleLobbiesCommand());
        logger.LogInformation("DeactivateStaleLobbiesFunction dispatched.");

        logger.LogInformation("RefreshScheduleFunction triggered.");
        await mediator.Send(new RefreshScheduleCommand());
        logger.LogInformation("RefreshScheduleCommand dispatched.");
    }
}
