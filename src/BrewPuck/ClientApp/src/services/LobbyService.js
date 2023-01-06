import HttpService from '@/services/HttpService';
const controller = "lobby";
class LobbyService {
    constructor() {
        this._http = new HttpService(controller);
    }
    async createLobby(name) {
        return this._http.post(`?name=${name}`, null);
    }
    async getLobbyByCode(code) {
        return this._http.get("", code);
    }
    async joinLobbyByCode(code, name) {
        return this._http.post(`join/${code}?name=${name}`, null);
    }
    async pickScored(pick, eventId) {
        return this._http.post(`pick/${pick.id}/score?eventId=${eventId}`, null);
    }
}
export default new LobbyService();
//# sourceMappingURL=LobbyService.js.map