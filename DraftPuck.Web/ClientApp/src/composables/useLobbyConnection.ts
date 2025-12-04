import { ref, type Ref } from 'vue'
import { differenceInSeconds } from 'date-fns'

export function useLobbyConnection(
  sendDebugMessage: (message: string, level: number) => void,
  initializeHub: () => Promise<void>,
  setGames: () => Promise<void>,
  getLobby: () => Promise<void>,
  getLobbyEvents: (lobbyId: string) => Promise<void>,
  stopHubConnection: () => Promise<void>,
  stopGamePolling: () => void,
  lobby: Ref<Lobby | undefined>
) {
  const lastLobbyRetrieval = ref<Date>(new Date())
  const checkActivityTimer = ref<number>()

  function initializeActivityChecker() {
    if (checkActivityTimer.value) window.clearInterval(checkActivityTimer.value)
    checkActivityTimer.value = window.setInterval(checkActivity, 3000)
  }

  async function checkActivity() {
    const secondsSinceLastLobbyRetrieval = Math.abs(differenceInSeconds(new Date(), lastLobbyRetrieval.value))
    if (secondsSinceLastLobbyRetrieval > 300) {
      sendDebugMessage(`${secondsSinceLastLobbyRetrieval} seconds since last retrieval.`, 2)
      await initializeLobbyConnection()
    }
  }

  async function initializeLobbyConnection(skipLobbyRetrieval = false) {
    sendDebugMessage('Initializing lobby connection.', 2)
    const promises = [initializeHub(), setGames()]
    if (!skipLobbyRetrieval) promises.push(getLobby())
    await Promise.all(promises)

    if (lobby.value) {
      await getLobbyEvents(lobby.value.id)
    }

    initializeActivityChecker()
  }

  async function pauseLobbyConnection() {
    sendDebugMessage('Pausing lobby connection.', 2)

    // stop polling for game updates
    stopGamePolling()

    // disconnect hub
    await stopHubConnection()

    // stop activity polling
    if (checkActivityTimer.value) window.clearInterval(checkActivityTimer.value)
  }

  function updateLastLobbyRetrieval() {
    lastLobbyRetrieval.value = new Date()
  }

  return {
    initializeLobbyConnection,
    pauseLobbyConnection,
    updateLastLobbyRetrieval,
    lastLobbyRetrieval
  }
}
