export default class CreateLobbyRequest {
  name: string
  picksPerTeam: number
  isBotAutoPickingEnabled: boolean
  bots: Array<Bot>
  gameIds: Array<number>

  constructor(name: string, picksPerTeam: number, isBotAutoPickingEnabled: boolean, bots: Array<Bot>, gameIds: Array<number>) {
    this.name = name
    this.picksPerTeam = picksPerTeam
    this.isBotAutoPickingEnabled = isBotAutoPickingEnabled
    this.bots = bots
    this.gameIds = gameIds
  }
}
