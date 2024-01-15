export default class CreateLobbyRequest {
  name: string
  picksPerTeam: number

  constructor(name: string, picksPerTeam: number) {
    this.name = name
    this.picksPerTeam = picksPerTeam
  }
}
