import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'nhl'

class NhlApiService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getSchedule(date: string): Promise<Schedule> {
      return this._http.get(`schedule/${date}`)
  }
  
  public async getGame(gameId: number): Promise<Game> {
    return this._http.get(`game/${gameId}`)
  }
}

export default new NhlApiService()
