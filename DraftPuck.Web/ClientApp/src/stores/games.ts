import { ref } from 'vue'
import { defineStore } from 'pinia'
import GameService from '@/services/GameService'

/**
 * Today's games. The server pushes each game after every poll (applyUpdate), and the lobby page fetches them all
 * whenever it (re)connects (load). Those can arrive in any order, so a copy older than the one held is ignored.
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

  /** Fetches every game, updating the ones held and dropping any the server no longer has (e.g. after the daily cutoff). */
  async function load() {
    const fetched = await GameService.getAllGames()
    const fetchedIds = new Set(fetched.map((g) => g.id))

    games.value = games.value.filter((g) => fetchedIds.has(g.id))
    fetched.forEach(applyUpdate)
  }

  return { games, applyUpdate, load }
})
