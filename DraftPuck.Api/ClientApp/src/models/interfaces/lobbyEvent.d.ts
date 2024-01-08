interface LobbyEvent {
  id: string
  timeUtc: Date
  title: string
  text: string
  subtext?: string
  playerId?: number
  player2Id?: number
  teamId?: number
  created: Date
  gameEventId?: number
  gameId?: number
  isSent: boolean
  lastSendAttempt?: Date
  sendAttempts: number
  lobbyId: string
  lobbyEventType: LobbyEventType
  lobbyMemberId: string
  lobbyMember2Id: string
  teamColor: string
}
