using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Worker.Functions.Common;

public class InitializeQueuesFunction
{
    private readonly ILogger<InitializeQueuesFunction> _logger;
    private readonly IConfiguration _configuration;

    public InitializeQueuesFunction(ILogger<InitializeQueuesFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(InitializeQueues))]
    public async Task InitializeQueues([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer)
    {
        var connectionString = _configuration["AzureWebJobsStorage"];

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("AzureWebJobsStorage connection string not found");
            return;
        }

        var queueNames = new[] { "achievement-check-queue", "nhl-queue" };

        foreach (var queueName in queueNames)
        {
            try
            {
                var queueClient = new QueueClient(connectionString, queueName);
                await queueClient.CreateIfNotExistsAsync();
                _logger.LogInformation("Queue '{QueueName}' initialized", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize queue '{QueueName}'", queueName);
            }
        }
    }
}
