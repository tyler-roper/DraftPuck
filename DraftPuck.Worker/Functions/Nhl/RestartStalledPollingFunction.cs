using DraftPuck.Application.Features.Games;
using MediatR;
using Microsoft.Azure.Functions.Worker;

namespace DraftPuck.Worker.Functions.Nhl;

/// <summary>
/// Restarts polling of any game whose chain of poll messages has stopped. Timer triggers run on one instance at a time,
/// so scaling out doesn't restart a game twice.
/// </summary>
public class RestartStalledPollingFunction(IMediator mediator)
{
    private const string EveryMinute = "0 * * * * *";

    [Function(nameof(RestartStalledPollingFunction))]
    public Task RestartStalledPolling([TimerTrigger(EveryMinute)] TimerInfo _)
        => mediator.Send(new RestartStalledPollingCommand());
}
