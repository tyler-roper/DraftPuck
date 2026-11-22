import { format } from 'date-fns'
import PeriodType from '@/enums/periodType'
import GameState from '@/enums/gameState'

export function getOrdinal(periodNumber: number, periodType: PeriodType) {
  if (periodType === PeriodType.Regulation) {
    if (periodNumber === 1) return '1st'
    if (periodNumber === 2) return '2nd'
    if (periodNumber === 3) return '3rd'
  } else if (periodType === PeriodType.Overtime) {
    return periodNumber > 4 ? `${periodNumber - 3}OT` : 'OT'
  } else if (periodType === PeriodType.Shootout) {
    return 'SO'
  }
}

/**
 * A game's clock as scoreboards show it: the start time until it starts, then the time left and period ("12:34 - 2nd",
 * or "SO" in a shootout), then "Final", naming the period when it went past regulation ("Final (OT)").
 */
export function formatGameClock(game: Pick<GameSummary, 'dateTime' | 'gameState' | 'period' | 'periodType' | 'timeRemainingInPeriod'>): string {
  if (game.gameState === GameState.Upcoming) return format(game.dateTime, 'p')

  const ordinal = getOrdinal(game.period, game.periodType)
  if (game.gameState === GameState.Live) return game.periodType === PeriodType.Shootout ? 'SO' : `${game.timeRemainingInPeriod} - ${ordinal}`

  return game.period > 3 ? `Final (${ordinal})` : 'Final'
}

/**
 * The order games are shown in a lobby: games still to finish before final ones, then the lobby's own games before
 * others, then by start time.
 */
export function sortGamesForLobby(games: Game[], lobbyGameIds: number[]): Game[] {
  const rank = (game: Game) => [game.gameState === GameState.Final ? 1 : 0, lobbyGameIds.includes(game.id) ? 0 : 1, game.dateTime.getTime()]

  return [...games].sort((a, b) => {
    const [rankA, rankB] = [rank(a), rank(b)]
    const i = rankA.findIndex((value, index) => value !== rankB[index])
    return i === -1 ? 0 : rankA[i] - rankB[i]
  })
}
