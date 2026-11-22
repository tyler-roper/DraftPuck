import { onMounted, watch, type Ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useUserStore } from '@/stores/user'

/**
 * The nickname a join or create form starts with: the account's for a signed-in player (last visit's until the session
 * is restored), otherwise the name the guest used last. It's filled in again once the session is restored, unless a
 * guest has typed their own since.
 *
 * Call `whenSignedIn()` before submitting: it waits for the session, so the form sends the right player's name.
 */
export function useLobbyFormNickname(nickname: Ref<string>) {
  const userStore = useUserStore()
  const { isLoggedIn, seemsLoggedIn, isReady, currentUser, rememberedUser } = storeToRefs(userStore)
  let filled: string | undefined

  function guestNickname() {
    try {
      const latestLobby: { name?: string } | undefined = JSON.parse(localStorage.getItem('latestLobby') ?? 'null') ?? undefined
      return currentUser.value?.nickname ?? latestLobby?.name ?? ''
    } catch {
      return currentUser.value?.nickname ?? ''
    }
  }

  function fill() {
    const value = isLoggedIn.value ? currentUser.value!.nickname! : seemsLoggedIn.value ? rememberedUser.value!.nickname! : guestNickname()
    // Only replace what this filled in (or nothing): never what a guest typed.
    if (nickname.value && nickname.value !== filled) return
    nickname.value = filled = value
  }

  onMounted(fill)
  watch(isReady, fill)

  return {
    async whenSignedIn() {
      await userStore.initialize()
      fill()
    }
  }
}
