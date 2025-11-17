import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'titles'

class TitleService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getAllTitles(): Promise<Array<Title>> {
    return this._http.get('')
  }
}

export default new TitleService()
