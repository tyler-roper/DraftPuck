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
}
