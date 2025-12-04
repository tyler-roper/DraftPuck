import { computed, type ComputedRef } from 'vue'
import * as SignalR from '@microsoft/signalr'

interface DebugCommandsContext {
  lobby: Lobby | undefined
  currentLobbyMember: LobbyMember | undefined
  hubConnection: SignalR.HubConnection | null
  setDebugging: (level: number) => void
  sendSystemMessage: (message: string) => void
  sendDebugMessage: (message: string, level: number) => void
}

export function useDebugCommands(context: ComputedRef<DebugCommandsContext>) {
  const getLobbyMemberInfo = (name?: string): Partial<LobbyMember> | undefined => {
    const lobbyMember = name
      ? context.value.lobby?.members.find((m) => m.name.toUpperCase() === name.toUpperCase())
      : context.value.currentLobbyMember

    if (!lobbyMember) return
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { messages, picks, ...lobbyMemberInfo } = lobbyMember
    return lobbyMemberInfo
  }

  const commands = computed<{ [command: string]: (...args: string[]) => void }>(() => ({
    debug: (level?: string) => {
      const newLevel = level == undefined || isNaN(+level) ? 1 : +level
      const newLevelClamped = Math.min(3, newLevel)
      context.value.setDebugging(newLevelClamped)
      if (newLevelClamped === 0) context.value.sendSystemMessage(`Debugging disabled.`)
      else context.value.sendSystemMessage(`Debugging enabled (Level ${newLevelClamped}).`)
    },
    connection: () => {
      if (!context.value.hubConnection) {
        context.value.sendSystemMessage('Hub connection is not initialized')
        return
      }
      context.value.sendSystemMessage(
        JSON.stringify(
          {
            id: context.value.hubConnection.connectionId,
            baseUrl: context.value.hubConnection.baseUrl,
            state: context.value.hubConnection.state
          },
          undefined,
          2
        )
      )
    },
    me: () => {
      context.value.sendSystemMessage(JSON.stringify(getLobbyMemberInfo(), undefined, 2))
    },
    user: (...nameParts: string[]) => {
      const name = nameParts.join(' ')
      const lobbyMember = context.value.lobby?.members.find((m) => m.name.toUpperCase() === name.toUpperCase())
      if (lobbyMember) {
        context.value.sendSystemMessage(JSON.stringify(getLobbyMemberInfo(name), undefined, 2))
      } else {
        context.value.sendSystemMessage(`User ${name} not found.`)
      }
    }
  }))

  function handleCommand(command: string, ...args: [string]) {
    if (commands.value[command]) commands.value[command](...args)
  }

  return {
    commands,
    handleCommand
  }
}
