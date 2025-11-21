export default class MessageViewModel {
  id?: string
  lobbyMemberName: string
  lobbyMemberId: string
  lobbyMemberUserId: string
  message: string
  sent: Date
  isSystem: boolean = false

  constructor(lobbyMember: LobbyMember, message: string, sent: Date, id?: string) {
    this.lobbyMemberName = lobbyMember.name
    this.lobbyMemberId = lobbyMember.id
    this.lobbyMemberUserId = lobbyMember.userId
    this.message = message
    this.sent = sent
    this.id = id
  }
}
