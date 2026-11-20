namespace DraftPuck.Application.Features.Games;

/// <summary>
/// Pushes what each poll changed in a game to connected clients, so lobby pages don't have to poll for scores.
/// Best effort: a failed push must not fail the poll. The next push won't apply to a client that missed this one, so
/// it fetches the game instead; clients also resync on reconnect.
/// </summary>
public class GameUpdatedClientHandler(IClientEventService clientEventService, ILogger<GameUpdatedClientHandler> logger) : INotificationHandler<GameUpdatedNotification>
{
    public async Task Handle(GameUpdatedNotification notification, CancellationToken ct)
    {
        try
        {
            await clientEventService.SendGameChanges(GameChangesDto.Between(notification.LastSent, notification.Game), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to push game {gameId} to clients.", notification.Game.Id);
        }
    }
}
