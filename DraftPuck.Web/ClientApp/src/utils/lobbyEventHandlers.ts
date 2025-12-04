import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'
import type { ToastOptions } from 'vue-toastification/dist/types/types'
import { TYPE } from 'vue-toastification'
import VHtmlToast from '@/components/VHtmlToast.vue'

export interface LobbyEventHandlerContext {
  lobby: Lobby | undefined
  currentLobbyMember: LobbyMember | undefined
  games: Game[]
  toast: (content: any, options?: ToastOptions) => void
  onDrinkAssignedToCurrentUser: (lobbyEvent: LobbyEvent) => void
}

export function createLobbyEventHandlers(context: LobbyEventHandlerContext) {
  const replaceTemplatedStrings = (lobbyEvent: LobbyEvent) =>
    parseLobbyEventText(lobbyEvent, context.lobby!, context.games)

  const lobbyEventToast = (lobbyEvent: LobbyEvent, options?: ToastOptions | undefined) => {
    return context.toast(
      {
        component: VHtmlToast,
        props: {
          title: lobbyEvent.title,
          message: replaceTemplatedStrings(lobbyEvent).text
        }
      },
      options
    )
  }

  function notifyDrinkAssigned(lobbyEvent: LobbyEvent) {
    const currentUserIsRecipient = lobbyEvent.lobbyMember2Id === context.currentLobbyMember?.id
    if (!currentUserIsRecipient) {
      lobbyEventToast(lobbyEvent)
      return
    }

    // "queue up" drink for current user
    context.onDrinkAssignedToCurrentUser(lobbyEvent)
  }

  function notifyDrinkAwarded(lobbyEvent: LobbyEvent) {
    const isCurrentLobbyMember = lobbyEvent.lobbyMemberId === context.currentLobbyMember?.id
    lobbyEventToast(lobbyEvent, { type: isCurrentLobbyMember ? TYPE.SUCCESS : TYPE.INFO })
  }

  function notifyUserJoined(lobbyEvent: LobbyEvent) {
    lobbyEventToast(lobbyEvent)
  }

  return {
    onDrinkAssigned: notifyDrinkAssigned,
    onDrinkAwarded: notifyDrinkAwarded,
    onUserJoined: notifyUserJoined
  }
}
