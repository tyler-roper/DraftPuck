namespace DraftPuck.Application.Features.Lobbies.Events;

/// <summary>
/// Moves drinks and feed events for a goal onto its new NHL play ID, so they're found by later corrections
/// (revokes look drinks up by play ID) and by the check that stops a goal being awarded twice.
/// </summary>
public class RenumberGoalHandler(IDbContext dbContext) : INotificationHandler<GoalRenumberedNotification>
{
    public async Task Handle(GoalRenumberedNotification n, CancellationToken ct)
    {
        var drinks = await dbContext.Drinks
            .Where(d => d.EventId == n.OldEventId && d.LobbyMemberPick.GameId == n.GameId)
            .ToListAsync(ct);
        drinks.ForEach(d => d.EventId = n.NewEventId);

        var lobbyEvents = await dbContext.LobbyEvents
            .Where(e => e.GameEventId == n.OldEventId && e.GameId == n.GameId)
            .ToListAsync(ct);
        lobbyEvents.ForEach(e => e.GameEventId = n.NewEventId);

        if (drinks.Count > 0 || lobbyEvents.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }
}
