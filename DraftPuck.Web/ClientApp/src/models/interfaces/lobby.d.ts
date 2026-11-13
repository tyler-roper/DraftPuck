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
  isActive: boolean
  // Whether the current user can remove any pick at any time (host or site admin), per the server.
  canManageAllPicks: boolean
}
