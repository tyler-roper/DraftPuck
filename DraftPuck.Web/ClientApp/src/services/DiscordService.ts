import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'discord'

class DiscordService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getLinkUrl(): Promise<{ url: string }> {
    return this._http.get('link')
  }
}

export default new DiscordService()
