import HttpService from '@/services/HttpService';
const controller = "lobby";
class LobbyService {
    constructor() {
        this._http = new HttpService(controller);
    }
    async createLobby(name) {
        return this._http.post(`?name=${name}`, null);
    }
    async getLobbyById(id) {
        return this._http.get("", id);
    }
    async joinLobbyByCode(code, name) {
        return this._http.post(`join/${code}?name=${name}`, null);
    }
}
export default new LobbyService();
//# sourceMappingURL=LobbyService.js.map