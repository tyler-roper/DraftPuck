namespace DraftPuck.Core.Services.Interfaces;

public interface ILobbyEventService
{
    public Task SendUserJoinedEvent(Lobby lobby, LobbyMember lobbyMember);

    public Task SendUserRejoinedEvent(Lobby lobby, LobbyMember lobbyMember);

    public Task SendUserRemovedEvent(Lobby lobby, LobbyMember lobbyMember);

    public Task SendUserNameChangedEvent(Lobby lobby, LobbyMember lobbyMember, string oldName);

    public Task SendNewPickEvent(Lobby lobby, LobbyMember lobbyMember, int gameId, int playerId, int teamId);

    public Task SendPickRemovedEvent(Lobby lobby, LobbyMember lobbyMember, int gameId, int playerId, int teamId);

    public Task SendDrinkAwardedEvent(Lobby lobby, LobbyMember lobbyMember, int gameId, int gameEventId, int playerId, int teamId);

    public Task SendDrinkAssignedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, int gameId, int gameEventId, int playerId, int teamId);

    public Task SendDrinkInvalidatedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, int gameId, int gameEventId, int playerId);

    public Task SendDrinkRemovedEvent(Lobby lobby, LobbyMember lobbyMember);

    public Task SendGoalChangedEvent(int gameId, int newPlayerId, int oldPlayerId, int teamId);

    public Task SendGoalRemovedEvent(int gameId, int playerId);

    public Task Broadcast(Lobby lobby, string message);

    public Task SendMessage(string joinCode, MessageModel message);
}
