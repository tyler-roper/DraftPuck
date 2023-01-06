import HttpService, { IHttpService } from '@/services/HttpService';

const controller = "";
const basePath = "https://statsapi.web.nhl.com/api/v1/";

class NhlApiService {
    private readonly _http: IHttpService;

    constructor() {
        this._http = new HttpService(controller, false, basePath);
    }

    public async getSchedule(date?: string): Promise<Schedule> {
        if (date) {
            return this._http.getWithParams("schedule", { startDate: date, endDate: date, expand: "schedule.scoringplays" });
        } else {
            return this._http.get("schedule");
        }
    }

    public async getPlayer(id: number): Promise<{ people: Array<Player> }> {
        return this._http.get("people", id.toString());
    }

    public async getGameData(gamePk: number): Promise<LiveGame> {
        return this._http.get(`game/${gamePk}/feed/live?cb=${Date.now()}`);
    }

    public async getGamePatch(gamePk: number, startTimecode: string): Promise<Array<{ diff: Array<PatchOperation> }> | LiveGame>  {
        return this._http.get(`game/${gamePk}/feed/live/diffPatch?startTimecode=${startTimecode}&cb=${Date.now()}`);
    }

    public async getPlayerSeasonStats(playerId: number): Promise<PlayerSeasonStats> {
        const response: PlayerStatsResponse = await this._http.get(`people/${playerId}/stats?stats=statsSingleSeason`);
        try {
            return response.stats[0].splits[0].stat;
        } catch {
            return { goals: 0, points: 0, assists: 0, games: 0 } as PlayerSeasonStats;
        }
    }
}

export default new NhlApiService();