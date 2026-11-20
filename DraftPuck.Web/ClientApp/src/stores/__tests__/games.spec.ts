import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useGamesStore } from '@/stores/games'
import GameService from '@/services/GameService'
import GameState from '@/enums/gameState'

vi.mock('@/services/GameService', () => ({ default: { getAllGames: vi.fn(), getGame: vi.fn() } }))

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

describe('games store: pushed changes', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(GameService.getGame).mockReset()
  })

  function liveGame(updatedSecond: number): Game {
    return { ...game(1, updatedSecond), plays: [], homeTeam: { score: 0 }, awayTeam: { score: 0 }, playerSummaries: [] } as unknown as Game
  }

  function changesTo(baseSecond: number | null, updatedSecond: number, homeScore = 1): GameChanges {
    return {
      id: 1,
      baseUpdatedUtc: baseSecond === null ? null : at(baseSecond),
      updatedUtc: at(updatedSecond),
      gameState: GameState.Live,
      playIds: [],
      plays: [],
      homeTeam: { score: homeScore, situations: [], strength: 5, roster: null },
      awayTeam: { score: 0, situations: [], strength: 5, roster: null },
      playerSummaries: null,
      formerPlayers: null
    } as unknown as GameChanges
  }

  it('applies changes to the copy they were made from, in place', async () => {
    const store = useGamesStore()
    store.applyUpdate(liveGame(0))
    const held = store.games[0]

    await store.applyChanges(changesTo(0, 10))

    expect(store.games[0]).toBe(held)
    expect(held.homeTeam.score).toBe(1)
    expect(GameService.getGame).not.toHaveBeenCalled()
  })

  it('ignores changes it already has', async () => {
    const store = useGamesStore()
    store.applyUpdate(liveGame(10))

    await store.applyChanges(changesTo(0, 10, 7))

    expect(store.games[0].homeTeam.score).toBe(0)
    expect(GameService.getGame).not.toHaveBeenCalled()
  })

  it('fetches the game after a missed push', async () => {
    const store = useGamesStore()
    store.applyUpdate(liveGame(0))
    vi.mocked(GameService.getGame).mockResolvedValue({ ...liveGame(20), homeTeam: { score: 2 } } as unknown as Game)

    await store.applyChanges(changesTo(10, 20))

    expect(GameService.getGame).toHaveBeenCalledWith(1)
    expect(store.games[0].homeTeam.score).toBe(2)
  })

  it('fetches a game it does not hold', async () => {
    const store = useGamesStore()
    vi.mocked(GameService.getGame).mockResolvedValue(liveGame(5))

    await store.applyChanges(changesTo(null, 5))

    expect(store.games.map((g) => g.id)).toEqual([1])
  })

  it('fetches once while pushes keep missing, and again if the fetch is older than one of them', async () => {
    const store = useGamesStore()
    store.applyUpdate(liveGame(0))
    let resolveFirst!: (g: Game) => void
    vi.mocked(GameService.getGame)
      .mockReturnValueOnce(new Promise((resolve) => (resolveFirst = resolve)))
      .mockResolvedValueOnce(liveGame(30))

    const first = store.applyChanges(changesTo(10, 20))
    await store.applyChanges(changesTo(20, 30))
    expect(GameService.getGame).toHaveBeenCalledTimes(1)

    resolveFirst(liveGame(20))
    await first

    expect(GameService.getGame).toHaveBeenCalledTimes(2)
    expect(store.games[0].updatedUtc).toEqual(at(30))
  })
})
