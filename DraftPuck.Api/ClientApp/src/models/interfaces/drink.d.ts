interface Drink {
  id: string
  lobbyMemberPickId: string
  recipientLobbyMemberId: string
  eventId: number
  created: Date
  assigned: Date | null
}
