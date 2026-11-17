using DraftPuck.Application.Features.Lobbies.Recap;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Worker.Functions.Common;

public class SendRecapNotificationsFunction(IMediator mediator, ILogger<SendRecapNotificationsFunction> logger)
{
    // Noon in New York is 16:00 UTC in summer and 17:00 in winter; the handler sends only on the right one. Not on startup:
    // a restart (or a deploy) would send the day's pushes again.
    private const string utc1600And1700 = "0 16,17 * * *";

    [Function("SendRecapNotificationsFunction")]
    public async Task SendRecapNotifications([TimerTrigger(utc1600And1700)] TimerInfo _)
    {
        logger.LogInformation("SendRecapNotificationsFunction triggered.");
        await mediator.Send(new SendRecapNotificationsCommand());
    }
}
