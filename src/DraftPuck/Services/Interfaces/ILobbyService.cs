namespace DraftPuck.Services.Interfaces
{
    public interface ILobbyService
    {
        public Task<Lobby> CreateLobby(Guid userId, NewLobbyRequest request);

        public Task<Lobby?> GetLobby(string joinCode, bool includeRemovedUsers = false);

        public Task<Lobby?> GetLobby(Guid lobbyId);

        public Task<List<LobbyEvent>> GetLobbyEvents(Guid userId, Guid lobbyId);

        public Task<bool> UserIsInLobby(Guid userId, Guid lobbyId);

        public Task<bool> UserIsInLobby(Guid userId, string joinCode);

        public Task<Lobby?> JoinLobbyByCode(Guid userId, string joinCode, JoinLobbyRequest request);

        public Task<LobbyMemberPick> MakePick(Guid userId, string joinCode, MakePickRequest request);

        public Task RemovePick(Guid currentUserId, string joinCode, Guid lobbyMemberPickId);

        public Task<Drink> AssignDrink(Guid userId, string joinCode, Guid drinkId, Guid recipientLobbyMemberId);

        public Task ChangeName(Guid userId, string joinCode, string newName);

        public Task RemoveLobbyMember(Guid currentUserId, string joinCode, Guid lobbyMemberId);

        public Task DeleteOldLobbies();

        public Task Broadcast(string joinCode, string message);
    }
}
