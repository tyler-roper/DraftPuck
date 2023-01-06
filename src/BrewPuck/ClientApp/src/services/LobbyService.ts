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

    public async joinLobbyByCode(code: string, name: string, isBot = false, botPickStyle?: BotPickStyle): Promise<Lobby> {
        return this._http.post(`${code}/join`, { name, isBot, botPickStyle });
    }

    public async makePick(code: string, gamePk: number, playerId: number, lobbyMemberId: string | null = null): Promise<LobbyMemberPick> {
        return this._http.post(`${code}/pick`, { gamePk, playerId, lobbyMemberId });
    }

    public async newDrink(code: string, lobbyMemberPickId: string, eventId: number): Promise<Drink> {
        return this._http.post(`${code}/drink`, { lobbyMemberPickId, eventId });
    }

    public listen(code: string) {
        return this._http.eventSource(`${code}/listen`);
    }

    public assignDrink(code: string, drinkId: string, recipientLobbyMemberId: string) {
        return this._http.post(`${code}/drink/${drinkId}/assign?recipientLobbyMemberId=${recipientLobbyMemberId}`, null);
    }
}

export default new LobbyService();