import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'auth'

class AuthService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async useRefreshToken(): Promise<AuthResponse> {
    return await this._http.post('refreshTokens/use', {})
  }

  public async login(email: string, password: string): Promise<AuthResponse> {
    return await this._http.post('login', { email, password })
  }

  public async logout(): Promise<void> {
    return await this._http.post('logout', undefined)
  }
}

export default new AuthService()
