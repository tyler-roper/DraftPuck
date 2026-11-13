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
  // Players who left the lineup (e.g. scratched). Not pickable; only used to name them in the feed.
  formerPlayers?: PlayerSummary[]
}
