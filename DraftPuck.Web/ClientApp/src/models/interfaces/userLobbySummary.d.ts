interface UserLobbySummary {
  id: string
  isActive: boolean
  joinCode: string
  status: number
  created: Date
  isBotAutoPickingEnabled: boolean
  createdBy: string
  gameCount: number
  memberCount: number
  drinksGiven: number
  drinksTaken: number
  drinksPending: number
}
