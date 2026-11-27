import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import LobbyService from '@/services/LobbyService'
import { lobbyToOpenOnLaunch, rejoinDismissKey } from '@/helpers/rejoinHelpers'
import { isShowingFirstPage } from '@/helpers/routerHelpers'

const DISMISSED_KEY = 'draftpuck.rejoinDismissed'

function readDismissed(): string | null {
  try {
    return sessionStorage.getItem(DISMISSED_KEY)
  } catch {
    return null
  }
}

function writeDismissed(key: string) {
  try {
    sessionStorage.setItem(DISMISSED_KEY, key)
  } catch {
    // Storage blocked (private mode): the dismissal lasts until the page reloads.
  }
}

export interface RejoinOptions {
  /**
   * Opens the lobby. Called without a tap when the app opened on this page and the player has one live lobby
   * (`lobbyToOpenOnLaunch`): a phone that closed the app mid-game reopens it at the home page.
   */
  openOnLaunch?: (lobby: LobbySummary) => void
}

/**
 * The live lobbies the user is in, for a quick rejoin from the home page. Loads once the session is known (guests have
 * lobbies too). Dismissing hides it for this visit, until the set of live lobbies changes.
 */
export function useRejoin({ openOnLaunch }: RejoinOptions = {}) {
  const { currentUser, isReady } = storeToRefs(useUserStore())
  const lobbies = ref<LobbySummary[]>([])
  const dismissedKey = ref(readDismissed())
  // Opening a lobby by itself: the bar stays hidden so it doesn't flash up on the way.
  const isOpening = ref(false)

  const isVisible = computed(
    () => !isOpening.value && lobbies.value.length > 0 && dismissedKey.value !== rejoinDismissKey(lobbies.value)
  )

  async function load() {
    try {
      lobbies.value = (await LobbyService.getActiveLobbies()).filter((l) => l.isActive)
    } catch (e) {
      // A quick shortcut: without it the home page still works.
      console.error('Failed to load live lobbies:', e)
      lobbies.value = []
    }

    // Checked once the lobbies are in, so a player who has moved on to another page meanwhile stays there.
    const launchLobby = openOnLaunch && isShowingFirstPage() ? lobbyToOpenOnLaunch(lobbies.value, dismissedKey.value) : undefined
    if (launchLobby) {
      isOpening.value = true
      openOnLaunch!(launchLobby)
    }
  }

  function dismiss() {
    const key = rejoinDismissKey(lobbies.value)
    dismissedKey.value = key
    writeDismissed(key)
  }

  watch(
    () => (isReady.value ? currentUser.value?.id : undefined),
    (userId) => {
      if (userId) load()
      else lobbies.value = []
    },
    { immediate: true }
  )

  return { lobbies, isVisible, dismiss }
}
