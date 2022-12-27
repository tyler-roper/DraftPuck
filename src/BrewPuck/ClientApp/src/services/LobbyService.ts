import HttpService, { IHttpService } from '@/services/HttpService';

const controller = "lobby";

class LobbyService {
    private readonly _http: IHttpService;

    constructor() {
        this._http = new HttpService(controller);
    }

    public async createLobby(name: string): Promise<Lobby> {
        return this._http.post(`?name=${name}`,null);
    }

    public async getLobbyByCode(code: string): Promise<Lobby> {
        return this._http.get("",code);
    }

    public async joinLobbyByCode(code: string, name: string): Promise<Lobby> {
        return this._http.post(`join/${code}?name=${name}`, null);
    }

    public async pickScored(pick: LobbyMemberPick, eventId: number): Promise<void> {
        return this._http.post(`pick/${pick.id}/score?eventId=${eventId}`, null);
    }
}

export default new LobbyService();