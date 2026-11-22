import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, nextTick } from 'vue'
import { createPinia, setActivePinia, type Pinia } from 'pinia'

const router = { replace: vi.fn(), push: vi.fn() }
vi.mock('vue-router', async (importOriginal) => ({ ...(await importOriginal<typeof import('vue-router')>()), useRouter: () => router }))
vi.mock('@/services/LobbyService', () => ({ default: { joinLobbyByCode: vi.fn() } }))

import { useLobbyEntry } from '@/composables/useLobbyEntry'
import { useUserStore } from '@/stores/user'
import { useLobbyStore } from '@/stores/lobby'
import { useFriendsStore } from '@/stores/friends'
import LobbyService from '@/services/LobbyService'

let pinia: Pinia
const ME = 'user-1'

function lobbyWith(memberUserIds: string[], isActive = true): Lobby {
  return {
    joinCode: 'ABCD',
    isActive,
    members: memberUserIds.map((userId) => ({ id: `m-${userId}`, userId, name: `Name ${userId}`, picks: [] }))
  } as unknown as Lobby
}

function signIn(isGuest = false) {
  const user = useUserStore()
  user.currentUser = { id: ME, isGuest, nickname: 'Sam' } as User
  user.isReady = true
}

function mount(lobbies: (Lobby | undefined)[]) {
  const lobbyStore = useLobbyStore()
  const refreshLobby = vi.fn(async () => {
    lobbyStore.lobby = lobbies.length > 1 ? lobbies.shift() : lobbies[0]
  })
  const connect = vi.fn(async () => {})
  const reportError = vi.fn()
  let api!: ReturnType<typeof useLobbyEntry>
  createApp({
    setup() {
      api = useLobbyEntry({ joinCode: 'ABCD', refreshLobby, connect, reportError })
      return () => null
    }
  })
    .use(pinia)
    .mount(document.createElement('div'))
  return { api, refreshLobby, connect, reportError }
}

const flush = async () => {
  for (let i = 0; i < 6; i++) await Promise.resolve()
  await nextTick()
}

describe('useLobbyEntry', () => {
  beforeEach(() => {
    localStorage.clear()
    pinia = createPinia()
    setActivePinia(pinia)
    vi.clearAllMocks()
  })

  it('lets a member straight in: remembers the lobby, answers its invite, and starts listening', async () => {
    signIn()
    const forgetInvite = vi.spyOn(useFriendsStore(), 'forgetInvite')
    const { api, connect } = mount([lobbyWith([ME])])
    await flush()

    expect(connect).toHaveBeenCalled()
    expect(forgetInvite).toHaveBeenCalledWith('ABCD')
    expect(JSON.parse(localStorage.getItem('latestLobby')!)).toEqual({ joinCode: 'ABCD', name: `Name ${ME}` })
    expect(api.isReady.value).toBe(true)
    expect(api.isInvalidLobby.value).toBe(false)
  })

  it('joins a signed-in player under their nickname first', async () => {
    signIn()
    const { connect } = mount([lobbyWith([]), lobbyWith([ME])])
    await flush()

    expect(LobbyService.joinLobbyByCode).toHaveBeenCalledWith('ABCD', 'Sam')
    expect(connect).toHaveBeenCalled()
  })

  it('asks a guest for a name, and lets them in once they join', async () => {
    signIn(true)
    const { api, connect } = mount([lobbyWith([]), lobbyWith([ME])])
    await flush()

    expect(api.isAskingForName.value).toBe(true)
    expect(api.isReady.value).toBe(false)
    expect(LobbyService.joinLobbyByCode).not.toHaveBeenCalled()
    expect(connect).not.toHaveBeenCalled()

    await api.onJoined()

    expect(api.isAskingForName.value).toBe(false)
    expect(connect).toHaveBeenCalled()
  })

  it('sends an ended lobby to its review', async () => {
    signIn()
    const { api, connect } = mount([lobbyWith([ME], false)])
    await flush()

    expect(router.replace).toHaveBeenCalledWith({ name: 'LobbyReview', params: { joinCode: 'ABCD' } })
    expect(api.isInvalidLobby.value).toBe(true)
    expect(connect).not.toHaveBeenCalled()
  })

  it("calls a lobby that doesn't exist invalid", async () => {
    signIn()
    const { api, connect } = mount([undefined])
    await flush()

    expect(api.isInvalidLobby.value).toBe(true)
    expect(api.isLoading.value).toBe(false)
    expect(connect).not.toHaveBeenCalled()
  })

  it('reports a failure and calls the lobby invalid', async () => {
    signIn()
    const { api, refreshLobby, reportError } = mount([lobbyWith([ME])])
    refreshLobby.mockRejectedValueOnce(new Error('down'))
    await flush()

    expect(reportError).toHaveBeenCalled()
    expect(api.isInvalidLobby.value).toBe(true)
    expect(api.isLoading.value).toBe(false)
  })
})
