import { onMounted, onUnmounted, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'
import { useFriendsStore } from '@/stores/friends'

/**
 * Keeps the friends store current for the app's lifetime: loads it when someone signs in, clears it when they sign
 * out, and refreshes it when the app comes back into view (requests and invites may have arrived meanwhile).
 * Friend pushes that arrive while the app is open refresh it too (see stores/firebase.ts).
 */
export function useFriendsSync() {
  const { isLoggedIn } = storeToRefs(useUserStore())
  const friends = useFriendsStore()

  const refresh = () => friends.load().catch((e) => console.error('Failed to load friends:', e))
  const onVisibilityChange = () => {
    if (document.visibilityState === 'visible' && isLoggedIn.value) refresh()
  }

  watch(
    isLoggedIn,
    (loggedIn) => {
      if (loggedIn) refresh()
      else friends.reset()
    },
    { immediate: true }
  )

  onMounted(() => document.addEventListener('visibilitychange', onVisibilityChange))
  onUnmounted(() => document.removeEventListener('visibilitychange', onVisibilityChange))
}
