interface LobbyEvent {
    lobbyId: string;
    type: LobbyEventType;
    entityId: string;
    time: Date;
}