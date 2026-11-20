// What a poll changed in a game, pushed by the server ("GameChanged") instead of the whole game. Applies only to the
// copy it was made from (baseUpdatedUtc); null means the whole game (the server had nothing to compare with).
// See helpers/gameChanges.ts.
interface GameChanges {
  id: number
  baseUpdatedUtc: Date | null
  updatedUtc: Date
  dateTime: Date
  gameType: GameType
  gameState: GameState
  period: number
  periodType: PeriodType
  minutesRemainingInPeriod: number
  secondsRemainingInPeriod: number
  timeRemainingInPeriod: string
  goalsByPeriod: PeriodSummary[]
  pickingOpensUtc: Date
  isPickingClosed: boolean
  canMembersRemovePicks: boolean
  homeTeam: GameTeamChanges
  awayTeam: GameTeamChanges
  // Every play's ID, in order; `plays` holds only the new or changed ones.
  playIds: number[]
  plays: Play[]
  // Null when unchanged.
  playerSummaries: PlayerSummary[] | null
  formerPlayers: PlayerSummary[] | null
}

interface GameTeamChanges {
  score: number
  situations: TeamSituation[]
  strength: number
  // Null when unchanged.
  roster: Player[] | null
}
