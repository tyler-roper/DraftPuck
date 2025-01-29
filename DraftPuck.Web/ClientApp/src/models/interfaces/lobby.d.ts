interface Lobby {
  id: string
  joinCode: string
  status: number
  picksPerTeam: number
  created: Date
  isBotAutoPickingEnabled: boolean
  createdBy: string
  gameIds: Array<number>
  members: Array<LobbyMember>
}
