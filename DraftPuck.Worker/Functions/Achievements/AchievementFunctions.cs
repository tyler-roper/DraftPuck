using Azure.Storage.Queues.Models;
using DraftPuck.Application.Features.Achievements;
using DraftPuck.Shared.Achievements;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DraftPuck.Worker.Functions.Achievements;

public class AchievementFunctions(ILogger<AchievementFunctions> logger, IMediator mediator)
{

    [Function(nameof(AchievementFunctions))]
    public async Task ProcessCheckAchievementMessage([QueueTrigger("achievement-check-queue")] QueueMessage queueMessage)
    {
        var messageString = queueMessage.Body.ToString();

        try
        {
            var message = JsonSerializer.Deserialize<CheckAchievementsMessage>(messageString);
            if (message == null)
            {
                logger.LogError("Queue message could not be deserialized or was null: {QueueItem}", messageString);
                return;
            }

            logger.LogInformation("Processing check for User {UserId} due to trigger {TriggerType}.", message.UserId, message.TriggerType);

            var command = new CheckAndAwardAllAchievementsCommand
            {
                UserId = message.UserId,
                TriggerType = message.TriggerType
            };

            await mediator.Send(command);

            logger.LogInformation("Successfully completed achievement check for User {UserId}.", message.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FATAL ERROR processing achievement check for message: {QueueItem}", messageString);
            throw;
        }
    }
}