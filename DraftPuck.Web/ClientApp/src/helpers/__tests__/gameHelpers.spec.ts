import { describe, expect, it } from 'vitest'
import { format } from 'date-fns'
import { formatGameClock, sortGamesForLobby } from '@/helpers/gameHelpers'
import GameState from '@/enums/gameState'
import PeriodType from '@/enums/periodType'

function game(id: number, startHourUtc: number, gameState = GameState.Upcoming): Game {
  return { id, gameState, dateTime: new Date(Date.UTC(2026, 10, 13, startHourUtc)) } as unknown as Game
}

const ids = (games: Game[]) => games.map((g) => g.id)

describe('sortGamesForLobby', () => {
  it("puts the lobby's games first, then the rest, each by start time", () => {
    const games = [game(1, 23), game(2, 0), game(3, 1), game(4, 22)]

    expect(ids(sortGamesForLobby(games, [3, 1]))).toEqual([3, 1, 2, 4])
  })

  it('puts final games last, even ones in the lobby', () => {
    const games = [game(1, 0, GameState.Final), game(2, 2), game(3, 1, GameState.Live)]

    expect(ids(sortGamesForLobby(games, [1]))).toEqual([3, 2, 1])
  })

  it("orders final games the same way: the lobby's first, then by start time", () => {
    const games = [game(1, 2, GameState.Final), game(2, 0, GameState.Final), game(3, 1, GameState.Final)]

    expect(ids(sortGamesForLobby(games, [3]))).toEqual([3, 2, 1])
  })

  it('does not reorder the original list', () => {
    const games = [game(1, 2), game(2, 1)]

    sortGamesForLobby(games, [])

    expect(ids(games)).toEqual([1, 2])
  })
})

describe('formatGameClock', () => {
  const start = new Date(Date.UTC(2026, 10, 22, 0, 30))
  const clock = (gameState: GameState, period = 0, periodType = PeriodType.Regulation, timeRemainingInPeriod = '12:34') =>
    formatGameClock({ dateTime: start, gameState, period, periodType, timeRemainingInPeriod })

  it('is the start time until the game starts', () => {
    expect(clock(GameState.Upcoming)).toBe(format(start, 'p'))
  })

  it('is the time left and the period while live', () => {
    expect(clock(GameState.Live, 2)).toBe('12:34 - 2nd')
    expect(clock(GameState.Live, 4, PeriodType.Overtime, '3:10')).toBe('3:10 - OT')
    expect(clock(GameState.Live, 6, PeriodType.Overtime, '0:45')).toBe('0:45 - 3OT')
  })

  it('is "SO" during a shootout', () => {
    expect(clock(GameState.Live, 5, PeriodType.Shootout)).toBe('SO')
  })

  it('is "Final" after regulation, and names the period after overtime or a shootout', () => {
    expect(clock(GameState.Final, 3)).toBe('Final')
    expect(clock(GameState.Final, 4, PeriodType.Overtime)).toBe('Final (OT)')
    expect(clock(GameState.Final, 5, PeriodType.Shootout)).toBe('Final (SO)')
  })
})
