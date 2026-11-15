interface Game {
  id: number
  dateTime: Date
  gameType: GameType
  gameState: GameState
  homeTeam: GameTeam
  awayTeam: GameTeam
  plays: Play[]
  period: number
  periodType: PeriodType
  minutesRemainingInPeriod: number
  secondsRemainingInPeriod: number
  timeRemainingInPeriod: string
  goalsByPeriod: PeriodSummary[]
  playerSummaries: PlayerSummary[]
  // When the server's poll produced this copy; the games store keeps the newest copy it has seen.
  updatedUtc: Date
  // Pick rules, decided by the server (see helpers/pickRules.ts).
  pickingOpensUtc: Date
  isPickingClosed: boolean
  canMembersRemovePicks: boolean
  // Players who left the lineup (e.g. scratched). Not pickable; only used to name them in the feed.
  formerPlayers?: PlayerSummary[]
}
