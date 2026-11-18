import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import LobbyService from '@/services/LobbyService'
import { rejoinDismissKey } from '@/helpers/rejoinHelpers'

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

/**
 * The live lobbies the user is in, for a quick rejoin from the home page. Loads once the session is known (guests have
 * lobbies too). Dismissing hides it for this visit, until the set of live lobbies changes.
 */
export function useRejoin() {
  const { currentUser, isReady } = storeToRefs(useUserStore())
  const lobbies = ref<LobbySummary[]>([])
  const dismissedKey = ref(readDismissed())

  const isVisible = computed(() => lobbies.value.length > 0 && dismissedKey.value !== rejoinDismissKey(lobbies.value))

  async function load() {
    try {
      lobbies.value = (await LobbyService.getActiveLobbies()).filter((l) => l.isActive)
    } catch (e) {
      // A quick shortcut: without it the home page still works.
      console.error('Failed to load live lobbies:', e)
      lobbies.value = []
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
