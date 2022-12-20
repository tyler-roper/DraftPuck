import HttpService from '@/services/HttpService';
const controller = "";
class NhlApiService {
    constructor() {
        this._http = new HttpService(controller);
    }
    async getSchedule(date) {
        if (date) {
            return this._http.getWithParams("schedule", { startDate: date, endDate: date });
        }
        else {
            return this._http.get("schedule");
        }
    }
    async getPlayer(id) {
        return this._http.get("people", id.toString());
    }
    async getGameData(gamePk) {
        return this._http.get(`game/${gamePk}/feed/live?cb=${Date.now()}`);
    }
    async getGamePatch(gamePk, startTimecode) {
        return this._http.get(`game/${gamePk}/feed/live/diffPatch?startTimecode=${startTimecode}&cb=${Date.now()}`);
    }
}
export default new NhlApiService();
//# sourceMappingURL=NhlApiService.js.map