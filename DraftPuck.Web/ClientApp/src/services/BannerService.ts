import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'banners'

class BannerService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getAllBanners(): Promise<Array<Banner>> {
    return this._http.get('')
  }
}

export default new BannerService()
