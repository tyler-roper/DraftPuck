import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'achievements'

class AchievementService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getAllAchievements(): Promise<Array<Achievement>> {
    return this._http.get('')
  }
}

export default new AchievementService()
