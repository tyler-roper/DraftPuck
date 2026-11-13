import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useGamesStore } from '@/stores/games'
import GameService from '@/services/GameService'
import GameState from '@/enums/gameState'

vi.mock('@/services/GameService', () => ({ default: { getAllGames: vi.fn() } }))

const at = (second: number) => new Date(Date.UTC(2026, 10, 13, 23, 0, second))

function game(id: number, updatedSecond: number, gameState = GameState.Live): Game {
  return { id, gameState, updatedUtc: at(updatedSecond) } as unknown as Game
}

describe('games store', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('adds a pushed game it has not seen', () => {
    const store = useGamesStore()

    store.applyUpdate(game(1, 0))

    expect(store.games.map((g) => g.id)).toEqual([1])
  })

  it('updates a held game in place, so references to it stay current', () => {
    const store = useGamesStore()
    store.applyUpdate(game(1, 0, GameState.Upcoming))
    const selected = store.games[0]

    store.applyUpdate(game(1, 10, GameState.Live))

    expect(store.games[0]).toBe(selected)
    expect(selected.gameState).toBe(GameState.Live)
  })

  it('ignores a copy that is not newer than the one held', () => {
    const store = useGamesStore()
    store.applyUpdate(game(1, 10, GameState.Live))

    store.applyUpdate(game(1, 5, GameState.Upcoming))
    store.applyUpdate(game(1, 10, GameState.Upcoming))

    expect(store.games[0].gameState).toBe(GameState.Live)
  })

  it('load keeps newer pushed copies and the same objects', async () => {
    const store = useGamesStore()
    store.applyUpdate(game(1, 20, GameState.Final))
    const held = store.games[0]
    vi.mocked(GameService.getAllGames).mockResolvedValue([game(1, 10, GameState.Live), game(2, 10)])

    await store.load()

    expect(store.games.map((g) => g.id)).toEqual([1, 2])
    expect(store.games[0]).toBe(held)
    expect(held.gameState).toBe(GameState.Final)
  })

  it('load drops games the server no longer has', async () => {
    const store = useGamesStore()
    store.applyUpdate(game(1, 0))
    store.applyUpdate(game(2, 0))
    vi.mocked(GameService.getAllGames).mockResolvedValue([game(2, 5)])

    await store.load()

    expect(store.games.map((g) => g.id)).toEqual([2])
  })
})
