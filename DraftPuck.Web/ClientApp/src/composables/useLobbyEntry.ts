import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useLobbyStore } from '@/stores/lobby'
import { useFriendsStore } from '@/stores/friends'
import LobbyService from '@/services/LobbyService'

// Remembered so the home page's join and create forms can prefill the code and name.
const LATEST_LOBBY_KEY = 'latestLobby'

export interface LobbyEntryOptions {
  joinCode: string
  // Loads the lobby into the lobby store.
  refreshLobby: () => Promise<void>
  // Starts listening for the lobby's pushes.
  connect: () => Promise<void>
  reportError: (error: unknown) => void
}

/**
 * Getting into a lobby's page, when it mounts: load the lobby, send an ended one to its review, join it if the player
 * isn't a member (signed-in players under their nickname; guests name themselves first, then call onJoined), and
 * start listening.
 */
export function useLobbyEntry({ joinCode, refreshLobby, connect, reportError }: LobbyEntryOptions) {
  const router = useRouter()
  const userStore = useUserStore()
  const friendsStore = useFriendsStore()
  const { isLoggedIn, currentUser } = storeToRefs(userStore)
  const { lobby, currentMember } = storeToRefs(useLobbyStore())

  const isLoading = ref(true)
  const isInvalidLobby = ref(false)
  // A guest opened the lobby's link without being a member: the page asks for their name, then joins.
  const isAskingForName = ref(false)
  const isReady = computed(() => !isLoading.value && !isAskingForName.value)

  onMounted(() =>
    load(async () => {
      await userStore.initialize()
      await refreshLobby()

      if (!lobby.value) return (isInvalidLobby.value = true)
      if (!lobby.value.isActive) {
        isInvalidLobby.value = true
        router.replace({ name: 'LobbyReview', params: { joinCode } })
        return
      }

      if (!currentMember.value) {
        if (!isLoggedIn.value) return (isAskingForName.value = true)
        await LobbyService.joinLobbyByCode(joinCode, currentUser.value!.nickname!)
        await refreshLobby()
      }

      await enter()
    })
  )

  /** The guest joined under the name they chose. */
  async function onJoined() {
    isAskingForName.value = false
    isLoading.value = true
    await load(async () => {
      await refreshLobby()
      await enter()
    })
  }

  async function load(steps: () => Promise<unknown>) {
    try {
      await steps()
    } catch (e) {
      reportError(e)
      isInvalidLobby.value = true
    } finally {
      isLoading.value = false
    }
  }

  async function enter() {
    if (!currentMember.value) return router.push({ name: 'Home' })
    try {
      localStorage.setItem(LATEST_LOBBY_KEY, JSON.stringify({ joinCode, name: currentMember.value.name }))
    } catch {
      // Not remembered (storage blocked).
    }
    // Being in the lobby answers any invite to it, however they joined.
    friendsStore.forgetInvite(joinCode)
    await connect()
  }

  return { isLoading, isInvalidLobby, isAskingForName, isReady, onJoined }
}
