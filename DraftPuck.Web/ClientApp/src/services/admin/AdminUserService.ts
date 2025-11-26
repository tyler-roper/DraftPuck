import HttpService, { type IHttpService } from '@/services/HttpService'
import { toQueryString } from '@/helpers/requestHelpers'

const controller = 'admin/users'

class AdminUserService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async getAll(request?: GetAllUsersRequest): Promise<User[]> {
    const queryString = toQueryString(request ?? {})
    return await this._http.get(queryString)
  }
}

export default new AdminUserService()
