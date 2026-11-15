import { describe, expect, it } from 'vitest'
import { sortGamesForLobby } from '@/helpers/gameHelpers'
import GameState from '@/enums/gameState'

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
