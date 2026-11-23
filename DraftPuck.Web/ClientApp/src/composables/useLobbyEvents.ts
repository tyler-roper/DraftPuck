import { useLobbyStore } from '@/stores/lobby'
import LobbyEventType from '@/enums/lobbyEventType'
import { parseAllDates } from '@/helpers/dateHelpers'
import { isCallout } from '@/helpers/calloutHelpers'

export interface LobbyEventReactions {
  /** Refetches the lobby, since most events mean it changed. */
  refreshLobby: () => Promise<void>
  /** Shows a drink someone gave you (the full-screen animation). */
  showDrinkForMe: (event: LobbyEvent) => void
  /** Shows a banner over the lobby: callouts (someone on fire, a rivalry...) and drinks earned or handed to others. */
  showBanner: (event: LobbyEvent) => void
  /** Shows the event as a toast. */
  notify: (event: LobbyEvent) => void
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

    // A merge moved a spot to another login. If it was this device's spot, this device is no longer in the lobby.
    if (event.lobbyEventType === LobbyEventType.MembersMerged && !store.currentMember)
      return reactions.onRemovedFromLobby('Your spot in this lobby moved to your other device.')

    parseAllDates(event)
    store.addLobbyEvent(event)

    if (isCallout(event.lobbyEventType)) return reactions.showBanner(event)

    switch (event.lobbyEventType) {
      // A drink for you gets the full-screen animation; everyone else sees it in a banner.
      case LobbyEventType.DrinkAssigned:
        if (event.lobbyMember2Id === me?.id) reactions.showDrinkForMe(event)
        else reactions.showBanner(event)
        break
      case LobbyEventType.DrinkAwarded:
        // A bot hands its drink out the moment it earns it, so its handout (just behind this) is the one to show.
        if (!store.lobby.members.find((m) => m.id === event.lobbyMemberId)?.isBot) reactions.showBanner(event)
        break
      // Tells the person on the new device which spot they're playing now.
      case LobbyEventType.UserJoined:
      case LobbyEventType.MembersMerged:
        reactions.notify(event)
        break
    }
  }

  return { onLobbyEvent }
}
