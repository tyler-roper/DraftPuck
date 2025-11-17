interface Play {
    id: number
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