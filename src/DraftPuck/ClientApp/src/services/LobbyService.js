import HttpService from '@/services/HttpService';
const controller = "lobby";
class LobbyService {
    constructor() {
        this._http = new HttpService(controller);
    }
    async createLobby(request) {
        return this._http.post("", request);
    }
    async getLobbyByCode(code) {
        return this._http.get("", code);
    }
    async getLobbyEventsById(lobbyId) {
        return this._http.get(`${lobbyId}/events`);
    }
    async joinLobbyByCode(code, name, isBot = false, botPickStyle) {
        return this._http.post(`${code}/join`, { name, isBot, botPickStyle });
    }
    async makePick(code, gamePk, playerId, teamId, lobbyMemberId = null) {
        return this._http.post(`${code}/pick`, { gamePk, playerId, lobbyMemberId, teamId });
    }
    async assignDrink(code, drinkId, recipientLobbyMemberId) {
        return this._http.post(`${code}/drink/${drinkId}/assign?recipientLobbyMemberId=${recipientLobbyMemberId}`, null);
    }
    async changeName(code, newName) {
        return this._http.post(`${code}/changeName?newName=${newName}`, null);
    }
}
export default new LobbyService();
//# sourceMappingURL=LobbyService.js.map