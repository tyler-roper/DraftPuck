import HttpService, { type IHttpService } from '@/services/HttpService'
import BotPickStyle from '@/enums/botPickStyle'
import type PickRequest from '@/models/pickRequest'
import type Bot from '@/models/bot'
import type CreateLobbyRequest from '@/models/createLobbyRequest'

const controller = 'lobbies'

class LobbyService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  public async createLobby(request: CreateLobbyRequest): Promise<Lobby> {
    return this._http.post('', request)
  }

  public async getLobbyByCode(code: string): Promise<Lobby> {
    return this._http.get('', code)
  }

  public async getLobbyEventsById(lobbyId: string): Promise<Array<LobbyEvent>> {
    return this._http.get(`${lobbyId}/events`)
  }

  public async addBot(code: string, bot: Bot) {
    return this.joinLobbyByCode(code, bot.name, true, bot.botPickStyle)
  }

  public async joinLobbyByCode(code: string, name: string, isBot = false, botPickStyle?: BotPickStyle): Promise<Lobby> {
    return this._http.post(`${code}/join`, { name, isBot, botPickStyle })
  }

  public async makePick(code: string, pickRequest: PickRequest): Promise<LobbyMemberPick> {
    return this._http.post(`${code}/pick`, pickRequest)
  }

  public async removeLobbyMember(code: string, lobbyMemberId: string): Promise<void> {
    return this._http.delete(`${code}/member/${lobbyMemberId}`)
  }

  public async removePick(code: string, pickId: string): Promise<void> {
    return this._http.delete(`${code}/pick/${pickId}`)
  }

  public async assignDrink(code: string, drinkId: string, recipientLobbyMemberId: string): Promise<Drink> {
    return this._http.post(`${code}/drink/${drinkId}/assign?recipientLobbyMemberId=${recipientLobbyMemberId}`, null)
  }

  public async changeName(code: string, newName: string): Promise<void> {
    return this._http.post(`${code}/changeName`, { newName })
  }
  
  public async sendMessage(code: string, message: Message): Promise<void> {
    return this._http.post(`${code}/message`, message)
  }

  public async leaveLobby(code: string): Promise<void> {
    return this._http.delete(`${code}/member/me`)
  }
}

export default new LobbyService()
