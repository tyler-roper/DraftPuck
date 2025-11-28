namespace DraftPuck.Application.Features.Lobbies.Picks;

public class BotPicksReadyNotificationHandler(IDbContext dbContext, IMediator mediator) : INotificationHandler<BotPicksReadyNotification>
{
    public async Task Handle(BotPicksReadyNotification notification, CancellationToken ct)
    {
        var game = notification.Game;
        var gameId = game.Id;
        var lobbies = await dbContext.Lobbies
            .Where(l => l.IsActive && l.LobbyMembers.Any(m => m.IsBot) && l.GameIds.Contains(game.Id))
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
            .AsNoTracking()
            .ToListAsync(ct);

        var lobbyChangedEvents = new List<LobbyStateChangedNotification>();

        foreach (var lobby in lobbies)
        {
            var picksMade = false;
            var allPicks = lobby.LobbyMembers
                .SelectMany(lm => lm.LobbyMemberPicks)
                .Where(pick => pick.IsActive && pick.GameId == game.Id);

            var pickedPlayerIds = allPicks.Select(pick => pick.PlayerId).ToHashSet();
            var bots = lobby.LobbyMembers.Where(lm => lm.IsBot && !lm.IsRemoved);

            foreach (var bot in bots)
            {
                var homePicksCount = allPicks.Count(p => p.LobbyMemberId == bot.Id
                                                         && p.IsActive
                                                         && p.TeamId == game.HomeTeam.Id);
                var awayPicksCount = allPicks.Count(p => p.LobbyMemberId == bot.Id
                                                         && p.IsActive
                                                         && p.TeamId == game.AwayTeam.Id);

                int homeRemaining = lobby.PicksPerTeam - homePicksCount;
                int awayRemaining = lobby.PicksPerTeam - awayPicksCount;

                if (homeRemaining <= 0 && awayRemaining <= 0)
                    continue;

                for (int i = 0; i < homeRemaining; i++)
                    await MakeBotPicks(bot, game, game.HomeTeam, pickedPlayerIds);

                for (int i = 0; i < awayRemaining; i++)
                    await MakeBotPicks(bot, game, game.AwayTeam, pickedPlayerIds);

                picksMade = true;
            }

            if (picksMade)
                lobbyChangedEvents.Add(new LobbyStateChangedNotification(new LobbyStateChangedPayload(lobby.JoinCode)));
        }

        await dbContext.SaveChangesAsync(ct);

        foreach (var lobbyChangedEvent in lobbyChangedEvents)
            await mediator.Publish(lobbyChangedEvent, ct);
    }

    private Task MakeBotPicks(LobbyMemberEntity bot, GameDto game, GameTeamDto team, HashSet<int> alreadyPicked)
    {
        var players = team.Roster;
        var availablePlayers = team.Roster.Where(player => !alreadyPicked.Contains(player.Id)).OrderByDescending(player => player.Goals).ToList();
        if (availablePlayers.Count == 0) return Task.CompletedTask;

        var strategy = GetStrategy(bot.BotPickStyle!.Value, availablePlayers.Count);
        var (start, end, fallback) = strategy;

        var preferred = availablePlayers
            .Skip(start)
            .Take(end - start + 1)
            .ToList();

        PlayerDto picked;

        if (preferred.Count > 0)
        {
            picked = RandomPick(preferred);
        }
        else
        {
            picked = fallback switch
            {
                BotFallbackPickStrategy.BestAvailable =>
                    availablePlayers.First(),

                BotFallbackPickStrategy.BestBelowRange =>
                    players
                        .Skip(end + 1)
                        .Where(p => availablePlayers.Select(p => p.Id).Contains(p.Id))
                        .DefaultIfEmpty(availablePlayers.Last())
                        .First(),

                BotFallbackPickStrategy.WorstAboveRange =>
                    players
                        .Take(start + 1)
                        .Where(p => availablePlayers.Select(p => p.Id).Contains(p.Id))
                        .DefaultIfEmpty(availablePlayers.First())
                        .Last(),

                BotFallbackPickStrategy.WorstAvailable =>
                    availablePlayers.Last(),

                BotFallbackPickStrategy.Random =>
                    RandomPick(availablePlayers),

                _ => availablePlayers.First()
            };
        }

        alreadyPicked.Add(picked.Id);

        dbContext.LobbyMemberPicks.Add(new LobbyMemberPickEntity
        {
            LobbyMemberId = bot.Id,
            GameId = game.Id,
            PlayerId = picked.Id,
            TeamId = team.Id
        });

        return Task.CompletedTask;
    }

    private static (int Start, int End, BotFallbackPickStrategy Fallback)
        GetStrategy(BotPickStyle style, int rosterCount)
        => style switch
        {
            // "Best": Pick best available player, always
            BotPickStyle.Best =>    (0, 0, BotFallbackPickStrategy.BestAvailable),

            // "Good": Pick randomly from 1 through 5. If none available, pick best remaining.
            BotPickStyle.Good =>    (0, 5, BotFallbackPickStrategy.BestAvailable),

            // "Average": Pick randomly between 5 and 10. If none available, pick best below 10.
            BotPickStyle.Average => (5, 10, BotFallbackPickStrategy.BestBelowRange),

            // "Bad": Pick randomly from the 5 worst players. If none available, pick worst available.
            BotPickStyle.Bad =>     (rosterCount - 6, rosterCount - 1, BotFallbackPickStrategy.WorstAvailable),

            // "Worst": Pick worst available player, always
            BotPickStyle.Worst =>   (rosterCount - 1, rosterCount - 1, BotFallbackPickStrategy.WorstAvailable),
            
            // "Random": Pick randomly from all available players
            BotPickStyle.Random =>  (0, rosterCount - 1, BotFallbackPickStrategy.Random),
            _ => (0, 0, BotFallbackPickStrategy.BestAvailable)
        };

    private static PlayerDto RandomPick(List<PlayerDto> players)
    {
        var i = Random.Shared.Next(players.Count);
        return players[i];
    }
}

