import { ref } from 'vue'
import { defineStore } from 'pinia'
import GameService from '@/services/GameService'
import { applyGameChanges } from '@/helpers/gameChanges'

/**
 * Today's games. The lobby page fetches them all whenever it (re)connects (load), and the server pushes what each poll
 * changed (applyChanges). Those can arrive in any order, so a copy older than the one held is ignored.
 *
 * Games are updated in place, never swapped for new objects, so anything holding a game (e.g. the selected game)
 * keeps seeing its latest state.
 */
export const useGamesStore = defineStore('games', () => {
  const games = ref<Game[]>([])

  /** Applies one game's latest state, unless an equal or newer copy is already held. */
  function applyUpdate(game: Game) {
    const existing = games.value.find((g) => g.id === game.id)
    if (!existing) {
      games.value.push(game)
      return
    }

    if (game.updatedUtc.getTime() <= existing.updatedUtc.getTime()) return
    Object.assign(existing, game)
  }

  /**
   * Applies a pushed poll's changes. If they can't be applied (the game isn't held, or this page missed the push
   * before), fetches the game instead.
   */
  async function applyChanges(changes: GameChanges) {
    const existing = games.value.find((g) => g.id === changes.id)
    if (existing && changes.updatedUtc.getTime() <= existing.updatedUtc.getTime()) return
    if (existing && applyGameChanges(existing, changes)) return
    await fetchGame(changes.id, changes.updatedUtc)
  }

  // Games being fetched, with the newest copy a push said exists. Pushes keep coming while a fetch is in flight; they
  // don't start another, but if the fetch comes back older than one of them, it fetches again.
  const fetching = new Map<number, number>()

  async function fetchGame(gameId: number, atLeast: Date) {
    const wanted = Math.max(fetching.get(gameId) ?? 0, atLeast.getTime())
    const inFlight = fetching.has(gameId)
    fetching.set(gameId, wanted)
    if (inFlight) return

    try {
      // The server saves a game before pushing it, so one fetch normally suffices; the cap is a safety net.
      for (let attempt = 0; attempt < 3; attempt++) {
        applyUpdate(await GameService.getGame(gameId))
        const held = games.value.find((g) => g.id === gameId)
        if (held && held.updatedUtc.getTime() >= fetching.get(gameId)!) return
      }
    } finally {
      fetching.delete(gameId)
    }
  }

  /** Fetches every game, updating the ones held and dropping any the server no longer has (e.g. after the daily cutoff). */
  async function load() {
    const fetched = await GameService.getAllGames()
    const fetchedIds = new Set(fetched.map((g) => g.id))

    games.value = games.value.filter((g) => fetchedIds.has(g.id))
    fetched.forEach(applyUpdate)
  }

  return { games, applyUpdate, applyChanges, load }
})
