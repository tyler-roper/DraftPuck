interface LobbyMember {
  id: string
  lobbyId: string
  userId: string
  name: string
  joined: Date
  picks: Array<LobbyMemberPick>
  isBot: boolean
  botPickStyle: BotPickStyle
  isRemoved: boolean
  messages: Array<Message>
}
