import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'games'

class NhlApiService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getAllGameSummaries(): Promise<GameSummary[]> {
      return this._http.get('summaries')
  }
  
  public async getGame(gameId: number): Promise<Game> {
    return this._http.get(`${gameId}`)
  }

  public async getAllGames(): Promise<Game[]> {
    return this._http.get('')
  }
}

export default new NhlApiService()
