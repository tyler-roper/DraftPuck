namespace DraftPuck.Application.Features.Lobbies;

public class LobbyEventHandler(IDbContext dbContext, IMediator mediator) :
    INotificationHandler<UserJoinedLobbyNotification>,
    INotificationHandler<UserRejoinedLobbyNotification>,
    INotificationHandler<UserNameChangedNotification>,
    INotificationHandler<UserRemovedNotification>,
    INotificationHandler<UserLeftNotification>,
    INotificationHandler<UserPromotedNotification>,
    INotificationHandler<PickMadeNotification>,
    INotificationHandler<PickRemovedNotification>,
    INotificationHandler<DrinkAssignedNotification>
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
            title: "Name Change", text: $"<strong>{n.Data.OldName}</strong> changed name to <strong>{n.Data.Member.Name}</strong>.");
    }

    public Task Handle(UserRemovedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserRemoved, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"<strong>{n.Data.Member.Name}</strong> was removed from the lobby.");
    }

    public Task Handle(UserLeftNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserLeft, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"<strong>{n.Data.Member.Name}</strong> left the lobby.");
    }

    public Task Handle(UserPromotedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.UserPromoted, ct, lobbyMemberId: n.Data.Member.Id,
            text: $"<strong>{n.Data.Member.Name}</strong> was promoted to lobby admin.");
    }

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

    public Task Handle(DrinkAssignedNotification n, CancellationToken ct)
    {
        return NewLobbyEvent(n.Data.Lobby, LobbyEventType.DrinkAssigned, ct, lobbyMemberId: n.Data.Sender.Id,
            lobbyMember2Id: n.Data.Recipient.Id, gameId: n.Data.Drink.LobbyMemberPick.GameId,
            playerId: n.Data.Drink.LobbyMemberPick.PlayerId, teamId: n.Data.Drink.LobbyMemberPick.TeamId,
            gameEventId: n.Data.Drink.EventId);
    }

    private async Task NewLobbyEvent(LobbyEntity lobby, LobbyEventType eventType, CancellationToken ct, Guid? lobbyMemberId = null, int? gameEventId = null, int? gameId = null, Guid? lobbyMember2Id = null, int? playerId = null, int? player2Id = null, int? teamId = null, string? title = null, string? text = null)
    {
        var lobbyEvent = new LobbyEventEntity
        {
            TimeUtc = DateTime.UtcNow,
            Title = title ?? LobbyEventTexts.GetTitle(eventType),
            Text = text ?? LobbyEventTexts.GetText(eventType),
            GameEventId = gameEventId,
            GameId = gameId,
            LobbyId = lobby.Id,
            LobbyEventType = eventType,
            LobbyMemberId = lobbyMemberId,
            LobbyMember2Id = lobbyMember2Id,
            PlayerId = playerId,
            Player2Id = player2Id,
            TeamId = teamId
        };

        dbContext.LobbyEvents.Add(lobbyEvent);
        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new LobbyEventCreatedNotification(lobbyEvent, lobby.JoinCode), ct);
    }
}