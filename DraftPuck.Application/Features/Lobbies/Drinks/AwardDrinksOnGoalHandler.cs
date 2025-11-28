namespace DraftPuck.Application.Features.Lobbies.Drinks;

public class AwardDrinksOnGoalHandler(IDbContext dbContext, IMediator mediator) : INotificationHandler<GoalScoredNotification>
{
    private static readonly Random _random = new();

    public async Task Handle(GoalScoredNotification notification, CancellationToken ct)
    {
        var data = notification.Data;

        var picksToReward = await dbContext.LobbyMemberPicks
            .Include(p => p.Drinks)
            .Include(p => p.LobbyMember)
                .ThenInclude(lm => lm.Lobby)
            .Where(p => p.IsActive
                && p.GameId == data.GameId
                && p.PlayerId == data.Scorer.Id
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
        var botAssignments = new List<AssignDrinkCommand>();

        foreach (var pick in picksToReward)
        {
            var drink = new DrinkEntity
            {
                LobbyMemberPickId = pick.Id,
                EventId = data.Play.Id
            };

            drinksToSave.Add(drink);
            drinkAwardedPayloads.Add(new DrinkAwardedPayload(pick.LobbyMember.Lobby, pick.LobbyMember, drink));

            // Handle bots AFTER we save drinks (we need drink id)
            if (pick.LobbyMember.IsBot)
            {
                var hasPossibleRecipients = lobbyMembersByLobbyId.TryGetValue(pick.LobbyMember.LobbyId, out var possibleRecipients) 
                    && possibleRecipients.Count > 0;

                if (hasPossibleRecipients)
                {
                    var recipient = possibleRecipients![_random.Next(possibleRecipients.Count)];

                    botAssignments.Add(new AssignDrinkCommand
                    {
                        Code = pick.LobbyMember.Lobby.JoinCode,
                        RecipientLobbyMemberId = recipient.Id,
                        AssignerUserId = pick.LobbyMember.UserId,
                        DrinkId = Guid.Empty // DrinkId gets filled later after SaveChanges
                    });
                }
            }
        }

        // Save all drinks in one round-trip
        dbContext.Drinks.AddRange(drinksToSave);
        await dbContext.SaveChangesAsync(ct);

        // Now drink IDs exist — publish notifications
        foreach (var drinkAwardedPayload in drinkAwardedPayloads)
            await mediator.Publish(new DrinkAwardedNotification(drinkAwardedPayload), ct);

        // Fix drink IDs for bot assignments & send them
        int drinkIndex = 0;
        foreach (var pick in picksToReward)
        {
            var drink = drinksToSave[drinkIndex];

            if (pick.LobbyMember.IsBot)
            {
                var assign = botAssignments.FirstOrDefault(a => a.DrinkId == Guid.Empty);
                if (assign != null)
                {
                    assign.DrinkId = drink.Id;
                    await mediator.Send(assign, ct);
                }
            }

            drinkIndex++;
        }
    }
}