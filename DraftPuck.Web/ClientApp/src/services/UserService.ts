import type CreateAccountViewModel from '@/models/interfaces/createAccountViewModel'
import HttpService, { type IHttpService } from '@/services/HttpService'
import type { NotificationPreferences } from '@/helpers/notificationSettings'

const controller = 'users'

class UserService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async signUp(createAccountViewModel: CreateAccountViewModel): Promise<User> {
    return await this._http.post('signup', createAccountViewModel)
  }

  public async createGuest(): Promise<AuthResponse> {
    return this._http.post('', null)
  }

  public async getUserById(id: string): Promise<User> {
    return this._http.get('', id)
  }

  /** A player's public profile (no email or settings). */
  public async getUserByName(name: string): Promise<UserProfile> {
    return this._http.get(`?name=${encodeURIComponent(name)}`)
  }

  public async updateUser(id: string, request: UpdateUserRequest): Promise<User> {
    return this._http.patch(id, request)
  }

  /** Registers this device (its push token) to get the current user's push notifications. */
  public async registerDevice(token: string, isInstalledApp: boolean): Promise<void> {
    await this._http.post('me/devices', { token, isInstalledApp })
  }

  /** Stops this device getting the current user's push notifications. */
  public async forgetDevice(token: string): Promise<void> {
    await this._http.post('me/devices/forget', { token })
  }

  /** Sets the current user's default notification settings, optionally applying them to their active lobbies. */
  public async updateNotificationPreferences(preferences: NotificationPreferences, applyToActiveLobbies: boolean): Promise<User> {
    return this._http.put('me/notificationPreferences', { ...preferences, applyToActiveLobbies })
  }
}

export default new UserService()
