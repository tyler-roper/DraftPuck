import HttpService, { IHttpService } from '@/services/HttpService';
import BotPickStyle from '@/enums/botPickStyle';

const controller = "lobby";
interface CreateLobbyRequest {
    name: string;
    picksPerTeam: number;
}


class LobbyService {
    private readonly _http: IHttpService;

    constructor() {
        this._http = new HttpService(controller);
    }

    public async createLobby(request: CreateLobbyRequest): Promise<Lobby> {
        return this._http.post("", request);
    }

    public async getLobbyByCode(code: string): Promise<Lobby> {
        return this._http.get("",code);
    }

    public async getLobbyEventsById(lobbyId: string): Promise<Array<LobbyEvent>> {
        return this._http.get(`${lobbyId}/events`);
    }

    public async joinLobbyByCode(code: string, name: string, isBot = false, botPickStyle?: BotPickStyle): Promise<Lobby> {
        return this._http.post(`${code}/join`, { name, isBot, botPickStyle });
    }

    public async makePick(code: string, gamePk: number, playerId: number, teamId: number, lobbyMemberId: string | null = null): Promise<LobbyMemberPick> {
        return this._http.post(`${code}/pick`, { gamePk, playerId, lobbyMemberId, teamId });
    }

    public async assignDrink(code: string, drinkId: string, recipientLobbyMemberId: string): Promise<Drink> {
        return this._http.post(`${code}/drink/${drinkId}/assign?recipientLobbyMemberId=${recipientLobbyMemberId}`, null);
    }

    public async changeName(code: string, newName: string): Promise<void> {
        return this._http.post(`${code}/changeName?newName=${newName}`, null);
    }
}

export default new LobbyService();