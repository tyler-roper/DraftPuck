import HttpService, { type IHttpService } from '@/services/HttpService'

const controller = 'friends'

class FriendService {
  private readonly _http: IHttpService

  constructor() {
    this._http = new HttpService(controller)
  }

  /** The current user's friends, friend requests both ways, and pending lobby invites. */
  public async getFriends(): Promise<FriendsList> {
    return this._http.get('')
  }

  /** Sends a friend request, or accepts theirs. */
  public async addFriend(userId: string): Promise<void> {
    await this._http.post(userId, null)
  }

  /** Unfriends, or declines or cancels a request. */
  public async removeFriend(userId: string): Promise<void> {
    await this._http.delete(userId)
  }

  /** Who has a pending invite to a lobby the current user is in. */
  public async getLobbyInvites(joinCode: string): Promise<string[]> {
    return this._http.get(`invites/${encodeURIComponent(joinCode)}`)
  }

  /** Invites friends to a lobby; resolves to everyone now invited to it. */
  public async sendLobbyInvites(joinCode: string, friendIds: string[]): Promise<string[]> {
    return this._http.post(`invites/${encodeURIComponent(joinCode)}`, { friendIds })
  }

  /** Dismisses the current user's invite to a lobby. */
  public async dismissLobbyInvite(joinCode: string): Promise<void> {
    await this._http.delete(`invites/${encodeURIComponent(joinCode)}`)
  }
}

export default new FriendService()
