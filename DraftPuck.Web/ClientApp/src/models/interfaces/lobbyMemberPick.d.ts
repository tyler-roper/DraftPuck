interface LobbyMemberPick {
  id: string
  lobbyMemberId: string
  playerId: number
  gameId: number
  teamId: number
  drinks: Array<Drink>
  created: Date
  isActive: boolean
}
