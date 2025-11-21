namespace DraftPuck.Application.Features.Lobbies;

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
                    .ThenInclude(l => l.LobbyMembers.Where(m => !m.IsBot && !m.IsRemoved))
            .Where(p => p.IsActive
                && p.GameId == data.GameId
                && p.PlayerId == data.Scorer.Id
                && !p.Drinks.Any(d => d.EventId == data.Play.Id))
            .ToListAsync(ct);

        if (picksToReward.Count == 0)
            return;

        foreach (var pick in picksToReward)
        {
            var drink = new DrinkEntity
            {
                LobbyMemberPickId = pick.Id,
                EventId = data.Play.Id
            };

            dbContext.Drinks.Add(drink);
            await dbContext.SaveChangesAsync(ct);

            var payload = new DrinkAwardedPayload(pick.LobbyMember.Lobby, pick.LobbyMember, drink);
            await mediator.Publish(new DrinkAwardedNotification(payload), ct);

            if (pick.LobbyMember.IsBot)
            {
                var potentialRecipients = pick.LobbyMember.Lobby.LobbyMembers
                    .Where(member => member.Id != pick.LobbyMemberId && !member.IsRemoved && !member.IsBot)
                    .ToList();

                if (potentialRecipients.Count == 0) continue;

                var recipient = potentialRecipients[_random.Next(potentialRecipients.Count)];
                var assignCmd = new AssignDrinkCommand
                {
                    Code = pick.LobbyMember.Lobby.JoinCode,
                    DrinkId = drink.Id,
                    RecipientLobbyMemberId = recipient.Id,
                    AssignerUserId = pick.LobbyMember.UserId
                };

                await mediator.Send(assignCmd, ct);
            }
        }
    }
}