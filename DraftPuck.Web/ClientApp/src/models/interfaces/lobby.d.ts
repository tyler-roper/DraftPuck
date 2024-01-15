interface Lobby {
  id: string
  joinCode: string
  status: number
  picksPerTeam: number
  created: Date
  createdBy: string
  members: Array<LobbyMember>
}
