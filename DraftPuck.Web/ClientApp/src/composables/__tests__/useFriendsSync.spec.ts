import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, nextTick } from 'vue'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import { useUserStore } from '@/stores/user'
import { useFriendsStore } from '@/stores/friends'
import { useFriendsSync } from '@/composables/useFriendsSync'
import FriendService from '@/services/FriendService'

vi.mock('@/services/FriendService', () => ({ default: { getFriends: vi.fn() } }))

let pinia: Pinia

function mountApp() {
  const app = createApp({
    setup() {
      useFriendsSync()
      return () => null
    }
  })
  app.use(pinia)
  app.mount(document.createElement('div'))
  return app
}

function signIn(isGuest = false) {
  const user = useUserStore()
  user.currentUser = { id: 'me', isGuest } as User
  user.isReady = true
}

const flush = async () => {
  await nextTick()
  await Promise.resolve()
}

describe('useFriendsSync', () => {
  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    vi.mocked(FriendService.getFriends).mockReset().mockResolvedValue({ friends: [], incoming: [{ id: 'a', nickname: 'A' }], outgoing: [], invites: [], suggestions: [] })
  })

  it('loads when someone signs in, and not for guests', async () => {
    const app = mountApp()
    signIn(true)
    await flush()
    expect(FriendService.getFriends).not.toHaveBeenCalled()

    signIn(false)
    await flush()
    expect(FriendService.getFriends).toHaveBeenCalledOnce()
    expect(useFriendsStore().pending).toBe(1)
    app.unmount()
  })

  it('clears the list on sign-out', async () => {
    signIn()
    const app = mountApp()
    await flush()
    expect(useFriendsStore().list).toBeDefined()

    useUserStore().currentUser = undefined
    await flush()
    expect(useFriendsStore().list).toBeUndefined()
    app.unmount()
  })

  it('refreshes when the app comes back into view', async () => {
    signIn()
    const app = mountApp()
    await flush()

    document.dispatchEvent(new Event('visibilitychange'))
    await flush()

    expect(FriendService.getFriends).toHaveBeenCalledTimes(2)
    app.unmount()
  })
})
