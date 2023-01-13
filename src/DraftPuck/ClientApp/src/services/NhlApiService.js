import HttpService from '@/services/HttpService';
const controller = "";
const basePath = "https://statsapi.web.nhl.com/api/v1/";
class NhlApiService {
    constructor() {
        this._http = new HttpService(controller, false, basePath);
    }
    async getSchedule(date) {
        if (date) {
            return this._http.getWithParams("schedule", { startDate: date, endDate: date, expand: "schedule.scoringplays" });
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
    async getPlayerSeasonStats(playerId) {
        const response = await this._http.get(`people/${playerId}/stats?stats=statsSingleSeason`);
        try {
            return response.stats[0].splits[0].stat;
        }
        catch {
            return { goals: 0, points: 0, assists: 0, games: 0 };
        }
    }
}
export default new NhlApiService();
//# sourceMappingURL=NhlApiService.js.map