interface LobbySummary {
  id: string
  isActive: boolean
  joinCode: string
  status: number
  created: Date
  isBotAutoPickingEnabled: boolean
  createdBy: string
  createdByName: string
  gameCount: number
  memberCount: number
  botCount: number
  guestCount: number
  drinksGiven: number
  drinksTaken: number
  drinksPending: number
}
