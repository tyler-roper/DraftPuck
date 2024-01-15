export default class PickRequest implements LobbyMemberPick {
  id: string
  lobbyMemberId: string
  playerId: number
  gameId: number
  teamId: number
  drinks: Array<Drink>
  created: Date
  isActive: boolean

  constructor(lobbyMemberId: string, playerId: number, gameId: number, teamId: number) {
    this.lobbyMemberId = lobbyMemberId
    this.playerId = playerId
    this.gameId = gameId
    this.teamId = teamId

    this.id = ''
    this.drinks = []
    this.created = new Date()
    this.isActive = true
  }
}
