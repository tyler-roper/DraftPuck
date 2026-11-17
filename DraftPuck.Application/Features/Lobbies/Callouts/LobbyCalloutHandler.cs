using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Events;

namespace DraftPuck.Application.Features.Lobbies.Callouts;

/// <summary>
/// Calls out notable moments in a lobby's feed as they happen: a member on fire, a drinking streak, a rivalry brewing,
/// picks gone ice cold. Callouts follow the feed events that cause them (so they appear after them), and their rules and
/// cooldowns read the database, so every instance agrees.
/// </summary>
public class LobbyCalloutHandler(IDbContext dbContext, LobbyEventWriter writer, ILogger<LobbyCalloutHandler> logger) :
    INotificationHandler<LobbyEventCreatedNotification>,
    INotificationHandler<GameUpdatedNotification>,
    INotificationHandler<GameFinishedNotification>
{
    /// <summary>On fire: a member's picks earn this many drinks within <see cref="OnFireWindow"/>.</summary>
    public const int OnFireDrinks = 3;
    public static readonly TimeSpan OnFireWindow = TimeSpan.FromMinutes(20);

    /// <summary>Drinking streak: a member is handed this many drinks within <see cref="StreakWindow"/>.</summary>
    public const int StreakDrinks = 3;
    public static readonly TimeSpan StreakWindow = TimeSpan.FromMinutes(10);

    /// <summary>A member isn't called out for the same thing again within this time.</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);

    /// <summary>Rivalry: two members have each given the other at least this many drinks. Called out once per lobby.</summary>
    public const int RivalryDrinksEachWay = 2;

    /// <summary>Ice cold: a member with at least this many picks in a game, none of which has scored...</summary>
    public const int IceColdPicks = 2;
    /// <summary>...with this little of the third period left (or once the game is over). Once per lobby and game.</summary>
    public const int IceColdMinutesLeft = 5;

    public async Task Handle(LobbyEventCreatedNotification n, CancellationToken ct)
    {
        var e = n.Event;
        if (e.LobbyId is not { } lobbyId) return;

        await Safely(async () =>
        {
            switch (e.LobbyEventType)
            {
                case LobbyEventType.DrinkAwarded when e.LobbyMemberId is { } member:
                    await OnFireAsync(lobbyId, n.JoinCode, member, ct);
                    break;
                case LobbyEventType.DrinkAssigned when e.LobbyMemberId is { } sender && e.LobbyMember2Id is { } recipient:
                    await DrinkingStreakAsync(lobbyId, n.JoinCode, recipient, ct);
                    await RivalryAsync(lobbyId, n.JoinCode, sender, recipient, ct);
                    break;
            }
        }, e.LobbyEventType);
    }

    public Task Handle(GameUpdatedNotification n, CancellationToken ct) => Safely(() => IceColdAsync(n.Game, ct), LobbyEventType.IceCold);

    public Task Handle(GameFinishedNotification n, CancellationToken ct) => Safely(() => IceColdAsync(n.Game, ct), LobbyEventType.IceCold);

    // --- On fire ---

    private async Task OnFireAsync(Guid lobbyId, string joinCode, Guid memberId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since = now - OnFireWindow;
        var earned = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.LobbyMemberId == memberId && d.LobbyMemberPick.IsActive && d.Created >= since)
            .Select(d => d.Created)
            .ToListAsync(ct);
        if (earned.Count < OnFireDrinks || await CalledOutRecentlyAsync(lobbyId, LobbyEventType.OnFire, memberId, now, ct)) return;

        const string name = NameToken;
        var detail = $"{earned.Count} drinks in {MinutesSince(earned.Min(), now)}";
        await writer.AddAsync(lobbyId, joinCode, LobbyEventType.OnFire, ct, lobbyMemberId: memberId, title: "On Fire",
            text: Pick($"{name} is on fire: {detail}!", $"{name} can't miss: {detail}.", $"Somebody cool {name} down: {detail}."));
    }

    // --- Drinking streak ---

    private async Task DrinkingStreakAsync(Guid lobbyId, string joinCode, Guid memberId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since = now - StreakWindow;
        var taken = await dbContext.Drinks
            .Where(d => d.RecipientLobbyMemberId == memberId && d.LobbyMemberPick.IsActive && d.Assigned >= since)
            .Select(d => d.Assigned!.Value)
            .ToListAsync(ct);
        if (taken.Count < StreakDrinks || await CalledOutRecentlyAsync(lobbyId, LobbyEventType.DrinkingStreak, memberId, now, ct)) return;

        const string name = NameToken;
        var detail = $"{taken.Count} drinks in {MinutesSince(taken.Min(), now)}";
        await writer.AddAsync(lobbyId, joinCode, LobbyEventType.DrinkingStreak, ct, lobbyMemberId: memberId, title: "Drinking Streak",
            text: Pick($"{name} has taken {detail}. Pace yourself!", $"Rough stretch for {name}: {detail}.",
                $"Someone get {name} some water: {detail}."));
    }

    // --- Rivalry ---

    private async Task RivalryAsync(Guid lobbyId, string joinCode, Guid sender, Guid recipient, CancellationToken ct)
    {
        if (sender == recipient) return;

        var exchanged = await dbContext.Drinks
            .Where(d => d.LobbyMemberPick.IsActive && (
                d.LobbyMemberPick.LobbyMemberId == sender && d.RecipientLobbyMemberId == recipient ||
                d.LobbyMemberPick.LobbyMemberId == recipient && d.RecipientLobbyMemberId == sender))
            .GroupBy(d => d.LobbyMemberPick.LobbyMemberId)
            .Select(g => new { From = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var given = exchanged.SingleOrDefault(x => x.From == sender)?.Count ?? 0;
        var back = exchanged.SingleOrDefault(x => x.From == recipient)?.Count ?? 0;
        if (given < RivalryDrinksEachWay || back < RivalryDrinksEachWay) return;

        var alreadyCalled = await dbContext.LobbyEvents.AnyAsync(e => e.LobbyId == lobbyId && e.LobbyEventType == LobbyEventType.RivalryBrewing &&
            (e.LobbyMemberId == sender && e.LobbyMember2Id == recipient || e.LobbyMemberId == recipient && e.LobbyMember2Id == sender), ct);
        if (alreadyCalled) return;

        const string a = NameToken, b = RecipientToken;
        var score = $"{given}–{back}";
        await writer.AddAsync(lobbyId, joinCode, LobbyEventType.RivalryBrewing, ct, lobbyMemberId: sender, lobbyMember2Id: recipient,
            title: "Rivalry Brewing",
            text: Pick($"A rivalry is brewing between {a} and {b}: {score}.", $"{a} and {b} won't stop sending each other drinks: {score}.",
                $"It's getting personal between {a} and {b}: {score}."));
    }

    // --- Ice cold ---

    private async Task IceColdAsync(GameDto game, CancellationToken ct)
    {
        var isOver = game.GameState == GameState.Final;
        var isLate = game.Period >= 3 && game.PeriodType == PeriodType.Regulation && game.MinutesRemainingInPeriod < IceColdMinutesLeft &&
            game.GameState == GameState.Live;
        if (!isOver && !isLate) return;

        var lobbies = await dbContext.Lobbies
            .Where(l => l.IsActive && l.GameIds.Contains(game.Id))
            .Where(l => !dbContext.LobbyEvents.Any(e => e.LobbyId == l.Id && e.LobbyEventType == LobbyEventType.IceCold && e.GameId == game.Id))
            .Select(l => new { l.Id, l.JoinCode })
            .ToListAsync(ct);

        foreach (var lobby in lobbies)
        {
            // The member with the most picks in the game, none of which has earned a drink.
            var coldest = await dbContext.LobbyMemberPicks
                .Where(p => p.IsActive && p.GameId == game.Id && p.LobbyMember.LobbyId == lobby.Id && !p.LobbyMember.IsRemoved)
                .GroupBy(p => new { p.LobbyMemberId, p.LobbyMember.Name })
                .Where(g => g.Count() >= IceColdPicks && g.All(p => !p.Drinks.Any()))
                .Select(g => new { g.Key.LobbyMemberId, g.Key.Name, Picks = g.Count() })
                .OrderByDescending(x => x.Picks).ThenBy(x => x.Name)
                .FirstOrDefaultAsync(ct);
            if (coldest == null) continue;

            const string name = NameToken;
            var matchup = $"{game.AwayTeam.Abbreviation} @ {game.HomeTeam.Abbreviation}";
            var text = isOver
                ? Pick($"{name} went 0 for {coldest.Picks} in {matchup}.", $"{coldest.Picks} picks in {matchup}, not one goal for {name}.")
                : Pick($"Under {IceColdMinutesLeft} minutes left in {matchup} and {name}'s {coldest.Picks} picks still haven't scored.",
                    $"{name}'s picks in {matchup} are ice cold: {coldest.Picks} picks, no goals.");
            await writer.AddAsync(lobby.Id, lobby.JoinCode, LobbyEventType.IceCold, ct, lobbyMemberId: coldest.LobbyMemberId, gameId: game.Id,
                title: "Ice Cold", text: text);
        }
    }

    // --- Shared ---

    private Task<bool> CalledOutRecentlyAsync(Guid lobbyId, LobbyEventType type, Guid memberId, DateTime now, CancellationToken ct)
    {
        var since = now - Cooldown;
        return dbContext.LobbyEvents.AnyAsync(e => e.LobbyId == lobbyId && e.LobbyEventType == type && e.LobbyMemberId == memberId && e.TimeUtc >= since, ct);
    }

    // Names are tokens the client fills in (as in other feed events), so a member who changes name shows their new one.
    private const string NameToken = "{{name}}";
    private const string RecipientToken = "{{recipientName}}";

    /// <summary>"7 minutes" (at least 1).</summary>
    private static string MinutesSince(DateTime start, DateTime now)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling((now - start).TotalMinutes));
        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }

    private static string Pick(params string[] texts) => texts[Random.Shared.Next(texts.Length)];

    /// <summary>A callout is a bonus: it never fails the event or game update that caused it.</summary>
    private async Task Safely(Func<Task> callout, LobbyEventType cause)
    {
        try
        {
            await callout();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Callouts failed after a {EventType} event.", cause);
        }
    }
}
