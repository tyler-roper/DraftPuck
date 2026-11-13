namespace DraftPuck.Application.Features.Lobbies.Events;

/// <summary>
/// Takes back the drinks credited to a scorer whose goal was reassigned or called back.
/// A drink already given to someone stays (they may have drunk it) and is announced as invalidated;
/// one nobody was given yet is deleted and announced as revoked.
/// </summary>
public class RevokeDrinksForGoalHandler(IDbContext dbContext, IMediator mediator) :
    INotificationHandler<GoalChangedNotification>,
    INotificationHandler<GoalRemovedNotification>
{
    public Task Handle(GoalChangedNotification n, CancellationToken ct)
        => RevokeAsync(n.Data.GameId, n.Data.Play.Id, n.Data.OldScorer.Id, ct);

    public Task Handle(GoalRemovedNotification n, CancellationToken ct)
        => RevokeAsync(n.Data.GameId, n.Data.EventId, n.Data.Scorer.Id, ct);

    private async Task RevokeAsync(int gameId, int eventId, int scorerId, CancellationToken ct)
    {
        var affectedDrinks = await dbContext.Drinks
            .Include(d => d.RecipientLobbyMember)
            .Include(d => d.LobbyMemberPick)
                .ThenInclude(lmp => lmp.LobbyMember)
                    .ThenInclude(lm => lm.Lobby)
            .Where(d => d.EventId == eventId
                && d.LobbyMemberPick.GameId == gameId
                && d.LobbyMemberPick.PlayerId == scorerId)
            .ToListAsync(ct);

        if (affectedDrinks.Count == 0) return;

        var notifications = new List<INotification>();
        var drinksToRemove = new List<DrinkEntity>();

        foreach (var drink in affectedDrinks)
        {
            var pick = drink.LobbyMemberPick;
            if (drink.RecipientLobbyMember is { IsRemoved: false } recipient)
            {
                notifications.Add(new DrinkInvalidatedNotification(
                    new DrinkInvalidatedPayload(pick.LobbyMember.Lobby, pick.LobbyMember, recipient, drink)));
            }
            else if (pick.IsActive)
            {
                drinksToRemove.Add(drink);
                notifications.Add(new DrinkRemovedNotification(
                    new DrinkRemovedPayload(pick.LobbyMember.Lobby, pick.LobbyMember, drink)));
            }
        }

        if (drinksToRemove.Count > 0)
        {
            dbContext.Drinks.RemoveRange(drinksToRemove);
            await dbContext.SaveChangesAsync(ct);
        }

        foreach (var notification in notifications)
            await mediator.Publish(notification, ct);
    }
}
