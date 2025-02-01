import type UpdateUserFcmRegistrationTokenRequest from '@/models/updateUserFcmRegistrationTokenRequest'
import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'users'

class UserService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async createUser(): Promise<User> {
    return this._http.post('', null)
  }

  public async getUserById(id: string): Promise<User> {
    return this._http.get('', id)
  }

  public async updateFcmRegistrationToken(id: string, request: UpdateUserFcmRegistrationTokenRequest): Promise<User> {
    return this._http.patch(`${id}/fcmtoken`, request);
  }

  public async updateNotificationPreferences(id: string, request: UserNotificationPreferencesRequestModel): Promise<User> {
    return this._http.patch(`${id}/notifications`, request);
  }
}

export default new UserService()
