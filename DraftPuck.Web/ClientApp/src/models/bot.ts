import type BotPickStyle from '@/enums/botPickStyle'

export default class Bot implements LobbyMember {
  id: string
  lobbyId: string
  userId: string
  name: string
  joined: Date
  picks: Array<LobbyMemberPick>
  isBot: boolean
  botPickStyle: BotPickStyle
  isRemoved: boolean
  messages: Message[]

  constructor(name: string, pickStyle: BotPickStyle) {
    this.id = ''
    this.lobbyId = ''
    this.userId = ''
    this.name = name
    this.joined = new Date()
    this.picks = []
    this.isBot = true
    this.botPickStyle = pickStyle
    this.isRemoved = false
    this.messages = []
  }
}
