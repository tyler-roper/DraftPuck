namespace DraftPuck.Application.Features.Lobbies.Drinks;

public class AwardDrinksOnGoalHandler(IDbContext dbContext, IMediator mediator, ILogger<AwardDrinksOnGoalHandler> logger) : INotificationHandler<GoalScoredNotification>
{

    public async Task Handle(GoalScoredNotification notification, CancellationToken ct)
    {
        var data = notification.Data;
        // Members can pick after puck drop, but a pick only earns drinks for goals that happen after it's made. This
        // matters when a goal is announced again: credited to a new scorer, or re-found after the cached game was lost.
        var goalHappenedUtc = data.Play.HappenedUtc;

        var picksToReward = await dbContext.LobbyMemberPicks
            .Include(p => p.Drinks)
            .Include(p => p.LobbyMember)
                .ThenInclude(lm => lm.Lobby)
            .Where(p => p.IsActive
                && p.GameId == data.GameId
                && p.PlayerId == data.Scorer.Id
                && p.Created <= goalHappenedUtc
                && !p.Drinks.Any(d => d.EventId == data.Play.Id))
            .ToListAsync(ct);

        if (picksToReward.Count == 0)
            return;

        var lobbyIdsWithBots = picksToReward
            .Where(p => p.LobbyMember.IsBot)
            .Select(p => p.LobbyMember.LobbyId)
            .Distinct()
            .ToList();

        var lobbyMembersByLobbyId = await dbContext.LobbyMembers
            .Where(lm => lobbyIdsWithBots.Contains(lm.LobbyId) && !lm.IsRemoved && !lm.IsBot)
            .GroupBy(lm => lm.LobbyId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList(), ct);

        var drinksToSave = new List<DrinkEntity>();
        var drinkAwardedPayloads = new List<DrinkAwardedPayload>();
        // Each bot assignment keeps a reference to its own drink, so its ID can be read after saving.
        var botAssignments = new List<(DrinkEntity Drink, AssignDrinkCommand Command)>();

        foreach (var pick in picksToReward)
        {
            var drink = new DrinkEntity
            {
                LobbyMemberPickId = pick.Id,
                EventId = data.Play.Id
            };

            drinksToSave.Add(drink);
            drinkAwardedPayloads.Add(new DrinkAwardedPayload(pick.LobbyMember.Lobby, pick.LobbyMember, drink));

            // Bots hand their drink to a random human in the lobby. Removed bots can't assign (AssignDrink only sees current members).
            if (pick.LobbyMember.IsBot
                && !pick.LobbyMember.IsRemoved
                && lobbyMembersByLobbyId.TryGetValue(pick.LobbyMember.LobbyId, out var possibleRecipients)
                && possibleRecipients.Count > 0)
            {
                var recipient = possibleRecipients[Random.Shared.Next(possibleRecipients.Count)];

                botAssignments.Add((drink, new AssignDrinkCommand
                {
                    Code = pick.LobbyMember.Lobby.JoinCode,
                    RecipientLobbyMemberId = recipient.Id,
                    AssignerUserId = pick.LobbyMember.UserId
                }));
            }
        }

        // Save all drinks in one round-trip
        dbContext.Drinks.AddRange(drinksToSave);
        await dbContext.SaveChangesAsync(ct);

        // Now drink IDs exist — publish notifications
        foreach (var drinkAwardedPayload in drinkAwardedPayloads)
            await mediator.Publish(new DrinkAwardedNotification(drinkAwardedPayload), ct);

        // A failed bot assignment only leaves that drink unassigned; it must not abort processing the rest of the game.
        foreach (var (drink, command) in botAssignments)
        {
            command.DrinkId = drink.Id;
            try
            {
                await mediator.Send(command, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Bot failed to assign drink {drinkId} in lobby {lobbyCode}.", drink.Id, command.Code);
            }
        }
    }
}
