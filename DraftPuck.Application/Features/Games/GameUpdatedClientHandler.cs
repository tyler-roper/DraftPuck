namespace DraftPuck.Application.Features.Games;

/// <summary>
/// Pushes each new copy of a game to connected clients, so lobby pages don't have to poll for scores.
/// Best effort: a failed push must not fail the poll, since the next poll pushes again and clients resync on reconnect.
/// </summary>
public class GameUpdatedClientHandler(IClientEventService clientEventService, ILogger<GameUpdatedClientHandler> logger) : INotificationHandler<GameUpdatedNotification>
{
    public async Task Handle(GameUpdatedNotification notification, CancellationToken ct)
    {
        try
        {
            await clientEventService.SendGameUpdated(notification.Game, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to push game {gameId} to clients.", notification.Game.Id);
        }
    }
}
