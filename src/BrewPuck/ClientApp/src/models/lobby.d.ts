interface Lobby {
    id: string;
    joinCode: string;
    status: number | null;
    lobbyMembers: Array<LobbyMember>;
}