using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Lobbies.Recap;

public class GetLobbyRecapQueryHandler(IDbContext dbContext, IGameCache gameCache, INhlClient nhlClient, ILobbyRecapCache recapCache,
    ILogger<GetLobbyRecapQueryHandler> logger)
    : IRequestHandler<GetLobbyRecapQuery, LobbyRecapDto>
{
    /// <summary>Achievements earned this long after the lobby was created count as earned that night.</summary>
    public static readonly TimeSpan Night = TimeSpan.FromDays(1);

    /// <summary>How long a finished lobby's recap is cached: briefly during its night (achievements can still arrive), then for weeks.</summary>
    public static readonly TimeSpan CacheDuringNight = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan CacheAfterNight = TimeSpan.FromDays(30);

    public async Task<LobbyRecapDto> Handle(GetLobbyRecapQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Lobby code is required.");

        var summary = await dbContext.Lobbies
            .Where(l => l.JoinCode == request.Code)
            .Select(l => new { l.Id, l.IsActive })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Lobby not found.");

        // A finished lobby's recap doesn't change (bar achievements during its night), and building it can take a dozen
        // NHL API calls for an old lobby.
        if (!summary.IsActive && await recapCache.GetAsync(summary.Id) is { } cached)
            return cached;

        var lobby = await dbContext.Lobbies
            .AsNoTracking()
            .Include(l => l.LobbyMembers.Where(m => !m.IsRemoved))
                .ThenInclude(m => m.LobbyMemberPicks.Where(p => p.IsActive))
                    .ThenInclude(p => p.Drinks)
            .AsSplitQuery()
            .SingleAsync(l => l.Id == summary.Id, ct);

        // All at once: an old lobby's games each come from the NHL API.
        var gameIds = lobby.LobbyMembers.SelectMany(m => m.LobbyMemberPicks).Select(p => p.GameId).Distinct().ToList();
        var loaded = await Task.WhenAll(gameIds.Select(GameAsync));
        var games = gameIds.Zip(loaded).ToDictionary(x => x.First, x => x.Second);

        var input = new RecapInput(
            [.. lobby.LobbyMembers.Select(m => new RecapMember(m.Id, m.Name, m.IsBot))],
            [.. lobby.LobbyMembers.SelectMany(m => m.LobbyMemberPicks).Select(p => new RecapPick(
                p.Id, p.LobbyMemberId, p.GameId, p.PlayerId, p.Drinks.Count, p.Created,
                SeasonGoals(games[p.GameId], p.PlayerId), games[p.GameId]?.DateTime))],
            [.. lobby.LobbyMembers.SelectMany(m => m.LobbyMemberPicks.SelectMany(p => p.Drinks)
                .Select(d => new RecapDrink(m.Id, d.RecipientLobbyMemberId, d.Created, d.Assigned)))],
            await MessageCountsAsync(lobby, ct),
            await AchievementsAsync(lobby, ct));

        var names = input.Members.ToDictionary(m => m.Id, m => m.Name);
        string NameOf(Guid? id) => id is { } i ? names.GetValueOrDefault(i, "someone") : "someone";
        string PlayerOf(RecapPick? pick) => pick == null ? "a player" : PlayerName(games.GetValueOrDefault(pick.GameId), pick.PlayerId);

        var awards = LobbyAwards.Compute(input, BitConverter.ToInt32(lobby.Id.ToByteArray(), 0))
            .Select(award => new LobbyAwardDto
            {
                Key = award.Key,
                Title = Titles[award.Key],
                MemberId = award.MemberId,
                OtherMemberId = award.Key == LobbyAwards.Rivalry ? award.OtherMemberId : null,
                MemberName = award.Key == LobbyAwards.Rivalry ? $"{NameOf(award.MemberId)} vs {NameOf(award.OtherMemberId)}" : NameOf(award.MemberId),
                Detail = award.Key switch
                {
                    LobbyAwards.BestPick => $"{PlayerOf(award.Pick)} earned {Drinks(award.Value)}",
                    LobbyAwards.HatTrick => $"{PlayerOf(award.Pick)} scored {award.Value} times",
                    LobbyAwards.Sleeper => $"{PlayerOf(award.Pick)} scored with {Count(award.Value, "goal")} on the season",
                    LobbyAwards.PerfectNight => $"All {award.Value} picks scored",
                    LobbyAwards.BuzzerBeater => award.Value switch
                    {
                        0 => $"Picked {PlayerOf(award.Pick)} at puck drop, and it paid off",
                        > 0 => $"Picked {PlayerOf(award.Pick)} {award.Value} min into the game, and it paid off",
                        _ => $"Picked {PlayerOf(award.Pick)} {-award.Value} min before puck drop, and it paid off"
                    },
                    LobbyAwards.IceCold => $"{award.Value} picks, not one goal",
                    LobbyAwards.Bully => $"Gave {NameOf(award.OtherMemberId)} {Drinks(award.Value)}",
                    LobbyAwards.Rivalry => $"Traded drinks all night: {award.Value}–{award.OtherValue}",
                    LobbyAwards.SpreadingTheLove => $"Gave drinks to {award.Value} different people",
                    LobbyAwards.Thirstiest => $"Took {Drinks(award.Value)}",
                    LobbyAwards.DesignatedDriver => $"Didn't take a drink all night ({award.Value} went around)",
                    LobbyAwards.Hoarder => $"Held drinks {award.Value} min on average before handing them out",
                    LobbyAwards.Chatterbox => $"{Count(award.Value, "message")} in the chat",
                    LobbyAwards.SilentAssassin => $"Not a word in the chat, but gave {Drinks(award.Value)}",
                    LobbyAwards.BeatenByARobot => $"Gave more drinks than every human: {award.Value}",
                    LobbyAwards.OverAchiever => $"Earned \"{award.Achievement!.Name}\": {award.Achievement.Description} ({RarityText(award.Achievement.Rarity)})",
                    _ => ""
                }
            })
            .ToList();

        var players = lobby.LobbyMembers.SelectMany(m => m.LobbyMemberPicks)
            .Select(p => (p.GameId, p.PlayerId)).Distinct()
            .Select(p => RecapPlayer(games[p.GameId], p.GameId, p.PlayerId))
            .OfType<RecapPlayerDto>()
            .ToList();

        var recap = new LobbyRecapDto { Awards = awards, Players = players };

        // Cache a finished lobby's recap, unless a game couldn't be loaded (the next view tries again).
        if (!lobby.IsActive && games.Values.All(g => g != null))
        {
            var duringNight = DateTime.UtcNow < lobby.Created + Night;
            await recapCache.SetAsync(lobby.Id, recap, duringNight ? CacheDuringNight : CacheAfterNight);
        }

        return recap;
    }

    private static readonly Dictionary<string, string> Titles = new()
    {
        [LobbyAwards.BestPick] = "Best pick",
        [LobbyAwards.Sleeper] = "Sleeper",
        [LobbyAwards.HatTrick] = "Hat trick",
        [LobbyAwards.PerfectNight] = "Perfect night",
        [LobbyAwards.BuzzerBeater] = "Buzzer beater",
        [LobbyAwards.IceCold] = "Ice cold",
        [LobbyAwards.Bully] = "Bully",
        [LobbyAwards.Rivalry] = "Rivalry",
        [LobbyAwards.SpreadingTheLove] = "Spreading the love",
        [LobbyAwards.Thirstiest] = "Thirstiest",
        [LobbyAwards.DesignatedDriver] = "Designated driver",
        [LobbyAwards.Hoarder] = "Hoarder",
        [LobbyAwards.Chatterbox] = "Chatterbox",
        [LobbyAwards.SilentAssassin] = "Silent assassin",
        [LobbyAwards.BeatenByARobot] = "Beaten by a robot",
        [LobbyAwards.OverAchiever] = "Over achiever"
    };

    private static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    private static string Drinks(int count) => Count(count, "drink");

    private static string RarityText(double rarity) =>
        rarity < 0.01 ? "fewer than 1% of players have it" : $"{Math.Round(rarity * 100)}% of players have it";

    private async Task<Dictionary<Guid, int>> MessageCountsAsync(LobbyEntity lobby, CancellationToken ct)
    {
        var memberIds = lobby.LobbyMembers.Select(m => m.Id).ToList();
        return await dbContext.Messages
            .Where(m => memberIds.Contains(m.LobbyMemberId) && !m.IsDeleted)
            .GroupBy(m => m.LobbyMemberId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    /// <summary>
    /// Achievements the members earned on the lobby's night, each with how rare it is among players with achievements. An
    /// achievement earned that night counts for every lobby the player was in that night.
    /// </summary>
    private async Task<List<RecapAchievement>> AchievementsAsync(LobbyEntity lobby, CancellationToken ct)
    {
        var memberByUser = lobby.LobbyMembers.Where(m => !m.IsBot).GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.First().Id);
        var userIds = memberByUser.Keys.ToList();
        var until = lobby.Created + Night;

        var earned = await dbContext.UserAchievements
            .AsNoTracking()
            .Where(ua => userIds.Contains(ua.UserId) && ua.DateEarned >= lobby.Created && ua.DateEarned < until)
            .Select(ua => new { ua.UserId, ua.AchievementId, ua.Achievement.FriendlyName, ua.Achievement.Description })
            .ToListAsync(ct);
        if (earned.Count == 0) return [];

        var achievementIds = earned.Select(e => e.AchievementId).Distinct().ToList();
        // Rarity among players who have earned anything, so guests who never play again don't make everything look rare.
        var players = Math.Max(1, await dbContext.UserAchievements.Select(ua => ua.UserId).Distinct().CountAsync(ct));
        var holders = await dbContext.UserAchievements
            .Where(ua => achievementIds.Contains(ua.AchievementId))
            .GroupBy(ua => ua.AchievementId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return [.. earned.Select(e => new RecapAchievement(memberByUser[e.UserId], e.FriendlyName, e.Description,
            Math.Min(1, (double)holders.GetValueOrDefault(e.AchievementId, 1) / players)))];
    }

    /// <summary>The day's games are cached; older ones come from the NHL API. A game that can't be loaded is skipped.</summary>
    private async Task<GameDto?> GameAsync(int gameId)
    {
        try
        {
            return await gameCache.GetGameByIdAsync(gameId) ?? await nhlClient.GetFullGameAsync(gameId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't load game {GameId} for a lobby recap.", gameId);
            return null;
        }
    }

    /// <summary>The player's goals on the season before tonight, from the roster (synced before the game), if known.</summary>
    private static int? SeasonGoals(GameDto? game, int playerId) =>
        game?.HomeTeam.Roster.Concat(game.AwayTeam.Roster).FirstOrDefault(p => p.Id == playerId)?.Goals;

    /// <summary>A picked player's full name and team, or null when the game couldn't be loaded.</summary>
    private static RecapPlayerDto? RecapPlayer(GameDto? game, int gameId, int playerId)
    {
        var player = game?.PlayerSummaries.Concat(game.FormerPlayers).FirstOrDefault(p => p.Id == playerId);
        if (game == null || player == null) return null;

        var team = player.TeamId == game.HomeTeam.Id ? game.HomeTeam.Abbreviation : game.AwayTeam.Abbreviation;
        return new RecapPlayerDto { GameId = gameId, PlayerId = playerId, Name = $"{player.FirstName} {player.LastName}", Team = team };
    }

    /// <summary>"McDavid (EDM)", or "a player" when the game couldn't be loaded.</summary>
    private static string PlayerName(GameDto? game, int playerId)
    {
        var player = game?.PlayerSummaries.Concat(game.FormerPlayers).FirstOrDefault(p => p.Id == playerId);
        if (game == null || player == null) return "a player";

        var team = player.TeamId == game.HomeTeam.Id ? game.HomeTeam.Abbreviation : game.AwayTeam.Abbreviation;
        return $"{player.LastName} ({team})";
    }
}
