import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'system'

class SystemService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getSettings(): Promise<SystemSettingsResponse> {
    return this._http.get('settings')
  }
}

export default new SystemService()
