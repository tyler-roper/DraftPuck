using Azure.Storage.Queues;
using System.Text.Json;

namespace DraftPuck.Infrastructure.AzureStorage;
public class NhlQueueService(QueueClient queueClient) : INhlQueueService
{
    public async Task SendMessageAsync<TMessage>(TMessage message, TimeSpan? delay = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        await queueClient.SendMessageAsync(json, visibilityTimeout: delay, cancellationToken: ct);
    }
}
