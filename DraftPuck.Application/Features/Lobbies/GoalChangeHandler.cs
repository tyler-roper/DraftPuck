namespace DraftPuck.Application.Features.Lobbies;

public class HandleGoalChangeHandler(IDbContext dbContext, IMediator mediator) : INotificationHandler<GoalChangedNotification>
{
    public async Task Handle(GoalChangedNotification notification, CancellationToken ct)
    {
        var data = notification.Data;
        var affectedDrinks = await dbContext.Drinks
            .Include(d => d.RecipientLobbyMember)
            .Include(d => d.LobbyMemberPick)
                .ThenInclude(lmp => lmp.LobbyMember)
                    .ThenInclude(lm => lm.Lobby)
            .Where(d => d.EventId == data.Play.Id && d.LobbyMemberPick.GameId == data.GameId)
            .ToListAsync(ct);

        if (affectedDrinks.Count == 0) return;

        var drinksToRemove = new List<DrinkEntity>();
        var invalidatedNotifications = new List<DrinkInvalidatedNotification>();
        var removedNotifications = new List<DrinkRemovedNotification>();

        foreach (var drink in affectedDrinks)
        {
            if (drink.LobbyMemberPick.PlayerId == data.OldScorer.Id)
            {
                if (drink.RecipientLobbyMember != null && !drink.RecipientLobbyMember.IsRemoved)
                {
                    var payload = new DrinkInvalidatedPayload(
                        drink.LobbyMemberPick.LobbyMember.Lobby,
                        drink.LobbyMemberPick.LobbyMember,
                        drink.RecipientLobbyMember,
                        drink);
                    invalidatedNotifications.Add(new DrinkInvalidatedNotification(payload));
                }
                else if (drink.LobbyMemberPick.IsActive)
                {
                    drinksToRemove.Add(drink);
                    var payload = new DrinkRemovedPayload(
                       drink.LobbyMemberPick.LobbyMember.Lobby,
                       drink.LobbyMemberPick.LobbyMember,
                       drink);
                    removedNotifications.Add(new DrinkRemovedNotification(payload));
                }
            }
        }

        if (drinksToRemove.Count != 0)
        {
            dbContext.Drinks.RemoveRange(drinksToRemove);
        }

        if (drinksToRemove.Count != 0 || invalidatedNotifications.Count != 0)
            await dbContext.SaveChangesAsync(ct);

        foreach (var n in invalidatedNotifications)
            await mediator.Publish(n, ct);

        foreach (var n in removedNotifications)
            await mediator.Publish(n, ct);
    }
}