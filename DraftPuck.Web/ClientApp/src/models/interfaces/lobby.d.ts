interface Lobby {
  id: string
  joinCode: string
  status: number
  picksPerTeam: number
  created: Date
  isBotAutoPickingEnabled: boolean
  createdBy: string
  members: Array<LobbyMember>
}
