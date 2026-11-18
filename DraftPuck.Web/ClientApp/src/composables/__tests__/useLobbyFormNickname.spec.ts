import { beforeEach, describe, expect, it } from 'vitest'
import { createApp, nextTick, ref } from 'vue'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import { useUserStore } from '@/stores/user'
import { useLobbyFormNickname } from '@/composables/useLobbyFormNickname'

let pinia: Pinia

/** Mounts a form using the composable, so onMounted runs; returns its nickname and whenSignedIn. */
function form() {
  const nickname = ref('')
  let api!: ReturnType<typeof useLobbyFormNickname>
  const app = createApp({
    setup() {
      api = useLobbyFormNickname(nickname)
      return () => null
    }
  })
  app.use(pinia)
  app.mount(document.createElement('div'))
  return { nickname, whenSignedIn: () => api.whenSignedIn() }
}

/** The session restores as this user when initialize() is called. */
function restoresAs(user: Partial<User>) {
  const store = useUserStore()
  store.initialize = async () => {
    store.currentUser = user as User
    store.isReady = true
    return store.currentUser
  }
}

describe('useLobbyFormNickname', () => {
  beforeEach(() => {
    localStorage.clear()
    pinia = createPinia()
    setActivePinia(pinia)
  })

  it("starts with last visit's account name, then the account's once the session is back", async () => {
    localStorage.setItem('draftpuck:profile', JSON.stringify({ id: 'u1', nickname: 'tyler', isGuest: false }))
    pinia = createPinia()
    setActivePinia(pinia)
    const { nickname, whenSignedIn } = form()
    expect(nickname.value).toBe('tyler')

    restoresAs({ id: 'u1', nickname: 'Tyler R', isGuest: false })
    await whenSignedIn()

    expect(nickname.value).toBe('Tyler R')
  })

  it("falls back to the guest's last name if the remembered session has ended", async () => {
    localStorage.setItem('draftpuck:profile', JSON.stringify({ id: 'u1', nickname: 'tyler', isGuest: false }))
    localStorage.setItem('latestLobby', JSON.stringify({ joinCode: 'ABCD', name: 'Puck Daddy' }))
    pinia = createPinia()
    setActivePinia(pinia)
    const { nickname, whenSignedIn } = form()

    restoresAs({ id: 'g1', isGuest: true })
    await whenSignedIn()
    await nextTick()

    expect(nickname.value).toBe('Puck Daddy')
  })

  it('never replaces a name a guest typed', async () => {
    const { nickname, whenSignedIn } = form()
    nickname.value = 'Wayne'

    restoresAs({ id: 'g1', isGuest: true })
    await whenSignedIn()

    expect(nickname.value).toBe('Wayne')
  })
})
