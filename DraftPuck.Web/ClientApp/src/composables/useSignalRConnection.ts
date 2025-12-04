import { ref, type Ref } from 'vue'
import * as SignalR from '@microsoft/signalr'
import { SignalRLogger } from '@/utils/SignalRLogger'

const HUB_URL = '/hub'

export function useSignalRConnection(sendDebugMessage: (message: string, level: number) => void) {
  const hubConnection: Ref<SignalR.HubConnection | null> = ref(null)

  function logError(error: string) {
    console.error(error)
    sendDebugMessage(error, 3)
    throw error
  }

  async function initializeHubConnection(
    joinCode: string,
    currentLobbyMember: LobbyMember | undefined,
    onLobbyEvent: (lobbyEvent: LobbyEvent) => void,
    onMessage: (message: Message) => void,
    onLobbyStateChanged: () => void
  ) {
    if (hubConnection.value && hubConnection.value.state === SignalR.HubConnectionState.Connected) {
      await hubConnection.value.stop()
    }

    const logger = new SignalRLogger(1)
    logger.onLog = sendDebugMessage

    const connection: SignalR.HubConnection = new SignalR.HubConnectionBuilder()
      .withUrl(HUB_URL, SignalR.HttpTransportType.ServerSentEvents)
      .configureLogging(logger)
      .withAutomaticReconnect()
      .build()

    hubConnection.value = connection

    hubConnection.value.on('LobbyEvent', onLobbyEvent)
    hubConnection.value.on('Message', onMessage)
    hubConnection.value.on('LobbyStateChanged', onLobbyStateChanged)
    hubConnection.value.onreconnecting(() =>
      sendDebugMessage(`Hub connection reconnecting... (State: ${hubConnection.value?.state})`, 2)
    )
    hubConnection.value.onreconnected(() =>
      sendDebugMessage(`Hub connection reconnected. (State: ${hubConnection.value?.state})`, 2)
    )

    try {
      await hubConnection.value.start()
      sendDebugMessage(`Hub connection started. (State: ${hubConnection.value.state})`, 2)
      await hubConnection.value.invoke('JoinLobby', joinCode, currentLobbyMember)
      sendDebugMessage(`Hub connection "Join Lobby" invoked. (State: ${hubConnection.value.state})`, 2)
    } catch (err) {
      logError(err as string)
    }
  }

  async function stopHubConnection() {
    if (hubConnection.value) {
      await hubConnection.value.stop()
    }
  }

  return {
    hubConnection: hubConnection as Ref<SignalR.HubConnection | null>,
    initializeHubConnection,
    stopHubConnection
  }
}
