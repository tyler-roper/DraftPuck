import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'system'

class SystemService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async reportError(error: any, info: string): Promise<void> {
    const errorObj = {
      message: error.message ?? '',
      stack: error.stack ?? ''
    }
    try {
      this._http.post('', { error: errorObj, info })
    } catch { }
  }
}

export default new SystemService()
