import HttpService, { type IHttpService } from '@/services/HttpService'
import type { AdminEventLogo, CreateEventLogoRequest } from '@/helpers/eventLogoHelpers'

const controller = 'admin/event-logos'

class AdminEventLogoService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  /** Every event logo, latest-starting first. */
  public async getAll(): Promise<AdminEventLogo[]> {
    return await this._http.get('')
  }

  public async create(request: CreateEventLogoRequest): Promise<AdminEventLogo> {
    return await this._http.post('', request)
  }

  public async remove(id: string): Promise<void> {
    await this._http.delete(id)
  }
}

export default new AdminEventLogoService()
