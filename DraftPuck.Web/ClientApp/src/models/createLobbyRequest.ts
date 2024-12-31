export default class CreateLobbyRequest {
  name: string
  picksPerTeam: number
  isBotAutoPickingEnabled: boolean

  constructor(name: string, picksPerTeam: number, isBotAutoPickingEnabled: boolean) {
    this.name = name
    this.picksPerTeam = picksPerTeam
    this.isBotAutoPickingEnabled = isBotAutoPickingEnabled
  }
}
