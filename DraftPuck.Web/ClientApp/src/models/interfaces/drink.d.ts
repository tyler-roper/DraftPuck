interface Drink {
  id: string
  lobbyMemberPickId: string
  recipientLobbyMemberId: string
  eventId: number
  created: Date
  assigned: Date | null
  /** What the giver said with it, if anything. */
  message?: string | null
}
