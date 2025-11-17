import type CreateAccountViewModel from '@/models/interfaces/createAccountViewModel'
import HttpService, { type IHttpService } from '@/services/HttpService'

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

  public async getUserByName(name: string): Promise<User> {
    return this._http.get(`?name=${name}`)
  }

  public async updateUser(id: string, request: UpdateUserRequest): Promise<User> {
    return this._http.patch(id, request)
  }

  public async getLobbies(id: string): Promise<Array<UserLobbySummary>> {
    return this._http.get(`${id}/lobbies`);
  }
}

export default new UserService()
