namespace DraftPuck.Services.Interfaces
{
    public interface ILobbyEventService
    {
        public Task SendUserJoinedEvent(Lobby lobby, LobbyMember lobbyMember);

        public Task SendUserRejoinedEvent(Lobby lobby, LobbyMember lobbyMember);

        public Task SendUserRemovedEvent(Lobby lobby, LobbyMember lobbyMember);

        public Task SendUserNameChangedEvent(Lobby lobby, LobbyMember lobbyMember, string oldName);

        public Task SendNewPickEvent(Lobby lobby, LobbyMember lobbyMember, long gamePk, long playerId, int teamId);

        public Task SendDrinkAwardedEvent(Lobby lobby, LobbyMember lobbyMember, long gamePk, int gameEventId, long playerId, int teamId);

        public Task SendDrinkAssignedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, long gamePk, int gameEventId, long playerId, int teamId);

        public Task SendDrinkInvalidatedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, long gamePk, int gameEventId, long playerId);

        public Task SendDrinkRemovedEvent(Lobby lobby, LobbyMember lobbyMember);

        public Task SendGoalChangedEvent(long gamePk, long newPlayerId, long oldPlayerId, int teamId);

        public Task SendGoalRemovedEvent(long gamePk, long playerId);
    }
}
