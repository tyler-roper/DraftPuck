using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Lobbies.Events;

public class LobbyEventHandler(IDbContext dbContext, LobbyEventWriter writer) :
    INotificationHandler<UserJoinedLobbyNotification>,
    INotificationHandler<UserRejoinedLobbyNotification>,
    INotificationHandler<UserNameChangedNotification>,
    INotificationHandler<UserRemovedNotification>,
    INotificationHandler<UserLeftNotification>,
    INotificationHandler<UserPromotedNotification>,
    INotificationHandler<PickMadeNotification>,
    INotificationHandler<PickRemovedNotification>,
    INotificationHandler<PickScratchedNotification>,
    INotificationHandler<BotPicksMadeNotification>,
    INotificationHandler<DrinkAssignedNotification>,
    INotificationHandler<DrinkAwardedNotification>,
    INotificationHandler<DrinkRemovedNotification>,
    INotificationHandler<DrinkInvalidatedNotification>,
    INotificationHandler<GoalChangedNotification>,
    INotificationHandler<GoalRemovedNotification>
{
    public Task Handle(UserJoinedLobbyNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserJoined, ct, lobbyMemberId: n.Data.Member.Id);
    }

    public Task Handle(UserRejoinedLobbyNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserRejoined, ct, lobbyMemberId: n.Data.Member.Id);
    }

    public Task Handle(UserNameChangedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserNameChanged, ct, lobbyMemberId: n.Data.Member.Id,
            title: "Name Change", text: $"{Bold(n.Data.OldName)} changed name to {Bold(n.Data.Member.Name)}.");
    }

    public Task Handle(UserRemovedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserRemoved, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"{Bold(n.Data.Member.Name)} was removed from the lobby.");
    }

    public Task Handle(UserLeftNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserLeft, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"{Bold(n.Data.Member.Name)} left the lobby.");
    }

    public Task Handle(UserPromotedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserPromoted, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"{Bold(n.Data.Member.Name)} was promoted to lobby admin.");
    }

    private static string Bold(string name) => LobbyEventWriter.Bold(name);

    public Task Handle(PickMadeNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.NewPick, ct, lobbyMemberId: n.Data.Member.Id,
            gameId: n.Data.Pick.GameId, playerId: n.Data.Pick.PlayerId, teamId: n.Data.Pick.TeamId);
    }

    public Task Handle(PickRemovedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.PickRemoved, ct, lobbyMemberId: n.Data.Member.Id,
            gameId: n.Data.Pick.GameId, playerId: n.Data.Pick.PlayerId, teamId: n.Data.Pick.TeamId);
    }

    public Task Handle(PickScratchedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.PickScratched, ct, lobbyMemberId: n.Data.Member.Id,
            gameId: n.Data.Pick.GameId, playerId: n.Data.Pick.PlayerId, teamId: n.Data.Pick.TeamId);
    }

    public async Task Handle(BotPicksMadeNotification n, CancellationToken ct)
    {
        // Named on the event when a single bot picked, as with other members' events.
        var onlyBot = n.Data.Picks.Count == 1 ? n.Data.Picks[0].Bot.Id : (Guid?)null;
        foreach (var (title, text) in BotPickEventTexts.Describe(n.Data))
            await NewLobbyEvent(n.Data.Lobby, LobbyEventType.BotPicksMade, ct, lobbyMemberId: onlyBot, gameId: n.Data.Game.Id,
                title: title, text: text);
    }

    public Task Handle(DrinkAwardedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.DrinkAwarded, ct, lobbyMemberId: n.Data.Member.Id,
            gameId: n.Data.Drink.LobbyMemberPick.GameId, playerId: n.Data.Drink.LobbyMemberPick.PlayerId, teamId: n.Data.Drink.LobbyMemberPick.TeamId,
            gameEventId: n.Data.Drink.EventId);
    }

    public Task Handle(DrinkAssignedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.DrinkAssigned, ct, lobbyMemberId: n.Data.Sender.Id,
            lobbyMember2Id: n.Data.Recipient.Id, gameId: n.Data.Drink.LobbyMemberPick.GameId,
            playerId: n.Data.Drink.LobbyMemberPick.PlayerId, teamId: n.Data.Drink.LobbyMemberPick.TeamId,
            gameEventId: n.Data.Drink.EventId);
    }

    public Task Handle(DrinkInvalidatedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.DrinkInvalidated, ct, lobbyMemberId: n.Data.Sender.Id,
            lobbyMember2Id: n.Data.Recipient.Id, gameId: n.Data.Drink.LobbyMemberPick.GameId,
            playerId: n.Data.Drink.LobbyMemberPick.PlayerId, teamId: n.Data.Drink.LobbyMemberPick.TeamId,
            gameEventId: n.Data.Drink.EventId);
    }

    public Task Handle(DrinkRemovedNotification n, CancellationToken ct)
    {
        // Nobody was given this drink yet, so there's no recipient to warn: it's "revoked", not "invalidated".
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.DrinkRevoked, ct, lobbyMemberId: n.Data.Member.Id,
            gameId: n.Data.Drink.LobbyMemberPick.GameId, playerId: n.Data.Drink.LobbyMemberPick.PlayerId,
            teamId: n.Data.Drink.LobbyMemberPick.TeamId, gameEventId: n.Data.Drink.EventId);
    }

    // Goal changes and removals are game events: every lobby with the game hears about them, whether or not anyone picked the scorer.
    public async Task Handle(GoalChangedNotification n, CancellationToken ct)
    {
        foreach (var lobby in await LobbiesWithGameAsync(n.Data.GameId, ct))
            await NewLobbyEvent(lobby, LobbyEventType.GoalChanged, ct, gameId: n.Data.GameId, gameEventId: n.Data.Play.Id,
                playerId: n.Data.NewScorer.Id, player2Id: n.Data.OldScorer.Id, teamId: n.Data.NewScorer.TeamId);
    }

    public async Task Handle(GoalRemovedNotification n, CancellationToken ct)
    {
        foreach (var lobby in await LobbiesWithGameAsync(n.Data.GameId, ct))
            await NewLobbyEvent(lobby, LobbyEventType.GoalRemoved, ct, gameId: n.Data.GameId, gameEventId: n.Data.EventId,
                playerId: n.Data.Scorer.Id, teamId: n.Data.Scorer.TeamId);
    }

    private Task<List<LobbyEntity>> LobbiesWithGameAsync(int gameId, CancellationToken ct)
        => dbContext.Lobbies.Where(l => l.GameIds.Contains(gameId)).ToListAsync(ct);

    private Task NewLobbyEvent(LobbyEntity lobby, LobbyEventType eventType, CancellationToken ct, Guid? lobbyMemberId = null, int? gameEventId = null, int? gameId = null, Guid? lobbyMember2Id = null, int? playerId = null, int? player2Id = null, int? teamId = null, string? title = null, string? text = null)
        => writer.AddAsync(lobby.Id, lobby.JoinCode, eventType, ct, lobbyMemberId, gameEventId, gameId, lobbyMember2Id, playerId, player2Id, teamId, title, text);
}