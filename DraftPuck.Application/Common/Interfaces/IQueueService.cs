namespace DraftPuck.Application.Common.Interfaces;
public interface IQueueService
{
    Task SendMessageAsync<T>(T message, TimeSpan? delay = null, CancellationToken ct = default);
}