interface Lobby {
    id: string;
    joinCode: string;
    status: number | null;
    members: Array<LobbyMember>;
}