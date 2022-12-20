import HttpService, { IHttpService } from '@/services/HttpService';
import { Schedule } from '@/models/schedule';
import { Players } from '@/models/players';
import { Game } from '@/models/game';

const controller = "";
const basePath = "https://statsapi.web.nhl.com/api/v1/";

class NhlApiService {
    private readonly _http: IHttpService;

    constructor() {
        this._http = new HttpService(controller, false, basePath);
    }

    public async getSchedule(date?: string): Promise<Schedule> {
        if (date) {
            return this._http.getWithParams("schedule", { startDate: date, endDate: date });
        } else {
            return this._http.get("schedule");
        }
    }

    public async getPlayer(id: number): Promise<Players> {
        return this._http.get("people", id.toString());
    }

    public async getGameData(gamePk: number): Promise<Game> {
        return this._http.get(`game/${gamePk}/feed/live?cb=${Date.now()}`);
    }

    public async getGamePatch(gamePk: number, startTimecode: string) {
        return this._http.get(`game/${gamePk}/feed/live/diffPatch?startTimecode=${startTimecode}&cb=${Date.now()}`);
    }
}

export default new NhlApiService();