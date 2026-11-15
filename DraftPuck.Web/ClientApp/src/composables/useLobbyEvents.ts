import { useLobbyStore } from '@/stores/lobby'
import LobbyEventType from '@/enums/lobbyEventType'
import { parseAllDates } from '@/helpers/dateHelpers'

export type LobbyEventTone = 'success' | 'info'

export interface LobbyEventReactions {
  /** Refetches the lobby, since most events mean it changed. */
  refreshLobby: () => Promise<void>
  /** Shows a drink someone gave you (the full-screen animation). */
  showDrinkForMe: (event: LobbyEvent) => void
  /** Shows the event as a toast. */
  notify: (event: LobbyEvent, tone?: LobbyEventTone) => void
  /** You were removed from the lobby, or left it (e.g. from another tab). */
  onRemovedFromLobby: (message: string) => void
  log?: (message: string, level: number) => void
}

/**
 * Handles lobby events pushed by the server: keeps the lobby and the feed's events current, and decides which events
 * the current member is told about, and how. The reactions themselves (toasts, animation, navigation) are the page's.
 */
export function useLobbyEvents(reactions: LobbyEventReactions) {
  const store = useLobbyStore()
  const log = reactions.log ?? (() => {})

  async function onLobbyEvent(event: LobbyEvent) {
    const me = store.currentMember
    log(`New ${LobbyEventType[event.lobbyEventType]} Event`, 3)
    log(JSON.stringify(event, undefined, 4), 2)

    const isAboutMe = event.lobbyMemberId === me?.id
    if (isAboutMe && event.lobbyEventType === LobbyEventType.UserRemoved) return reactions.onRemovedFromLobby('You were removed from the lobby.')
    if (isAboutMe && event.lobbyEventType === LobbyEventType.UserLeft) return reactions.onRemovedFromLobby('You left the lobby.')

    await reactions.refreshLobby()
    if (!store.lobby) return

    parseAllDates(event)
    store.addLobbyEvent(event)

    switch (event.lobbyEventType) {
      case LobbyEventType.DrinkAssigned:
        if (event.lobbyMember2Id === me?.id) reactions.showDrinkForMe(event)
        else reactions.notify(event)
        break
      case LobbyEventType.DrinkAwarded:
        reactions.notify(event, isAboutMe ? 'success' : 'info')
        break
      case LobbyEventType.UserJoined:
        reactions.notify(event)
        break
    }
  }

  return { onLobbyEvent }
}
