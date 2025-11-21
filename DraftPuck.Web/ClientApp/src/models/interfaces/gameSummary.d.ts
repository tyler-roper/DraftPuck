interface GameSummary {
  id: number
  dateTime: Date
  gameType: GameType
  gameState: GameState
  homeTeam: Team
  awayTeam: Team
  period: number
  periodType: PeriodType
  minutesRemainingInPeriod: number
  secondsRemainingInPeriod: number
  timeRemainingInPeriod: string
}
