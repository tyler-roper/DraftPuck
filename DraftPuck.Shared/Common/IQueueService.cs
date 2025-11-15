namespace DraftPuck.Shared.Common;
public interface IQueueService
{
    Task SendMessageAsync<T>(T message, TimeSpan? delay = null, CancellationToken ct = default);
}