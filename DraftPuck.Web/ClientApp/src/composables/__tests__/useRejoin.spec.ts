import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, nextTick } from 'vue'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import { useUserStore } from '@/stores/user'
import { useRejoin } from '@/composables/useRejoin'
import LobbyService from '@/services/LobbyService'

vi.mock('@/services/LobbyService', () => ({ default: { getActiveLobbies: vi.fn() } }))

let pinia: Pinia
const lobby = (joinCode: string, isActive = true) =>
  ({ joinCode, isActive, createdBy: 'me', createdByName: 'Me', memberCount: 2, gameCount: 1 }) as LobbySummary

function mount() {
  let api!: ReturnType<typeof useRejoin>
  createApp({
    setup() {
      api = useRejoin()
      return () => null
    }
  })
    .use(pinia)
    .mount(document.createElement('div'))
  return api
}

function signIn() {
  const user = useUserStore()
  user.currentUser = { id: 'me', isGuest: true } as User
  user.isReady = true
}

const flush = async () => {
  await nextTick()
  await Promise.resolve()
  await Promise.resolve()
}

describe('useRejoin', () => {
  beforeEach(() => {
    sessionStorage.clear()
    pinia = createPinia()
    setActivePinia(pinia)
    vi.mocked(LobbyService.getActiveLobbies)
      .mockReset()
      .mockResolvedValue([lobby('ABCD'), lobby('OLD1', false)])
  })

  it('waits for the session, then offers the live lobbies (guests too)', async () => {
    const rejoin = mount()
    await flush()
    expect(LobbyService.getActiveLobbies).not.toHaveBeenCalled()

    signIn()
    await flush()

    expect(rejoin.lobbies.value.map((l) => l.joinCode)).toEqual(['ABCD'])
    expect(rejoin.isVisible.value).toBe(true)
  })

  it('stays dismissed for the same lobbies, and comes back for a new one', async () => {
    signIn()
    const rejoin = mount()
    await flush()

    rejoin.dismiss()
    expect(rejoin.isVisible.value).toBe(false)
    expect(mount().isVisible.value).toBe(false)

    vi.mocked(LobbyService.getActiveLobbies).mockResolvedValue([lobby('ABCD'), lobby('EFGH')])
    const later = mount()
    await flush()
    expect(later.isVisible.value).toBe(true)
  })

  it('shows nothing when the lobbies fail to load', async () => {
    vi.mocked(LobbyService.getActiveLobbies).mockRejectedValue(new Error('offline'))
    vi.spyOn(console, 'error').mockImplementation(() => {})
    signIn()
    const rejoin = mount()
    await flush()

    expect(rejoin.isVisible.value).toBe(false)
  })
})
