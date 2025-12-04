import { ref } from 'vue'
import GameService from '@/services/GameService'
import GameState from '@/enums/gameState'

const ACTIVE_GAME_POLLING_INTERVAL_MS = 10000
const INACTIVE_GAME_POLLING_INTERVAL_MS = 60000

export function useGamePolling(sendDebugMessage: (message: string, level: number) => void) {
  const games = ref<Game[]>([])
  const timers = ref<number[]>([])

  const isGameInProgress = (game: Game) => game.gameState === GameState.Live
  const isGameOver = (game: Game) => game.gameState === GameState.Final
  const isGameStale = (game: Game) => isGameOver(game)

  function logError(error: string) {
    console.error(error)
    sendDebugMessage(error, 3)
    throw error
  }

  async function updateGame(gameId: number) {
    const gameIndex = games.value.findIndex((g) => gameId === g.id)
    const updatedGame = await GameService.getGame(gameId)
    Object.assign(games.value[gameIndex], updatedGame)
  }

  async function pollForUpdates(game: Game, attempts: number = 1) {
    const MAX_ATTEMPTS = 5
    const DEBOUNCE_ENABLED = false

    const msgPrefix = `[${game.awayTeam.abbreviation} @ ${game.homeTeam.abbreviation}]`
    let isSuccess = false
    let tryAgainIfFailure = attempts < MAX_ATTEMPTS

    try {
      await updateGame(game.id)
      isSuccess = true
    } catch (e) {
      logError(`${msgPrefix} ${e as string}`)
    }

    if (isGameStale(game)) {
      sendDebugMessage(`${msgPrefix} Game is stale. `, 1)
      return
    }

    let interval = isGameInProgress(game) ? ACTIVE_GAME_POLLING_INTERVAL_MS : INACTIVE_GAME_POLLING_INTERVAL_MS
    let debugMessage = `${msgPrefix} Updated. (Next update in ${interval / 1000} seconds)`

    if (!isSuccess && !tryAgainIfFailure) {
      debugMessage = `${msgPrefix} Stopping updates after ${MAX_ATTEMPTS} failed attempts.`
    } else if (!isSuccess) {
      const nextAttempt = attempts + 1
      if (DEBOUNCE_ENABLED) interval *= nextAttempt
      debugMessage = `${msgPrefix} Failed to update on attempt #${attempts}. (Trying again in ${interval / 1000} seconds)`
      timers.value.push(window.setTimeout(() => pollForUpdates(game, nextAttempt), interval))
    } else if (isSuccess) {
      timers.value.push(window.setTimeout(() => pollForUpdates(game), interval))
    }

    sendDebugMessage(debugMessage, 1)
  }

  async function setGames() {
    timers.value.forEach((t) => window.clearTimeout(t))

    sendDebugMessage(`Setting games...`, 1)
    games.value = await GameService.getAllGames()
    sendDebugMessage(`${games.value.length} games retrieved.`, 1)
    games.value.forEach((game) => {
      if (!isGameStale(game)) timers.value.push(window.setTimeout(() => pollForUpdates(game), ACTIVE_GAME_POLLING_INTERVAL_MS))
    })
  }

  function stopPolling() {
    timers.value.forEach((t) => window.clearTimeout(t))
    timers.value = []
  }

  return {
    games,
    setGames,
    stopPolling,
    isGameInProgress,
    isGameOver,
    isGameStale
  }
}
