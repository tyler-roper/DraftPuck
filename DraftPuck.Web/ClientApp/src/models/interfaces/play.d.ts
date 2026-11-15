interface Play {
  id: number
  /** When the play happened (UTC): when the server saw it, or an estimate for plays from before it was watching. */
  dateTime: Date
  period: number
  periodType: PeriodType
  timeInPeriod: string
  timeRemainingInPeriod: string
  type: PlayType
  primaryPlayerId: number | null
  primaryTeamId: number | null
  homeScore: number
  awayScore: number
  penalty: string | null
}
