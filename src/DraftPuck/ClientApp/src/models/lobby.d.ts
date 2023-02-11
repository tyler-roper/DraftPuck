interface Lobby {
    id: string;
    joinCode: string;
    status: number;
    picksPerTeam: number;
    created: Date;
    createdBy: string;
    members: Array<LobbyMember>;
}

interface LobbyMember {
    id: string;
    lobbyId: string;
    userId: string;
    name: string;
    joined: Date;
    picks: Array<LobbyMemberPick>
    isBot: boolean;
    botPickStyle: BotPickStyle;
    isRemoved: boolean;
}

interface LobbyMemberPick {
    id: string;
    lobbyMemberId: string;
    playerId: number;
    gamePk: number;
    teamId: number;
    drinks: Array<Drink>;
    created: Date;
    isActive: boolean;
}

interface Drink {
    id: string;
    lobbyMemberPickId: string;
    recipientLobbyMemberId: string;
    eventId: number;
    created: Date;
    assigned: Date | null;
}