export default class CreateLobbyRequest {
  name: string
  picksPerTeam: number
  isBotAutoPickingEnabled: boolean
  gameIds: Array<number>

  constructor(name: string, picksPerTeam: number, isBotAutoPickingEnabled: boolean, gameIds: Array<number>) {
    this.name = name
    this.picksPerTeam = picksPerTeam
    this.isBotAutoPickingEnabled = isBotAutoPickingEnabled
    this.gameIds = gameIds
  }
}
