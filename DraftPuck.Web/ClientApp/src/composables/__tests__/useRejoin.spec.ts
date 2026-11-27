import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, nextTick } from 'vue'
import { createPinia, setActivePinia, type Pinia } from 'pinia'
import { useUserStore } from '@/stores/user'
import { useRejoin } from '@/composables/useRejoin'
import LobbyService from '@/services/LobbyService'
import { START_LOCATION, type RouteLocationNormalized } from 'vue-router'
import { recordNavigation } from '@/helpers/routerHelpers'

vi.mock('@/services/LobbyService', () => ({ default: { getActiveLobbies: vi.fn() } }))

let pinia: Pinia
const lobby = (joinCode: string, isActive = true) =>
  ({ joinCode, isActive, createdBy: 'me', createdByName: 'Me', memberCount: 2, gameCount: 1 }) as LobbySummary

function mount(options: Parameters<typeof useRejoin>[0] = {}) {
  let api!: ReturnType<typeof useRejoin>
  createApp({
    setup() {
      api = useRejoin(options)
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
    recordNavigation(START_LOCATION)
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

  describe('when the app opens on the page', () => {
    it('opens the one live lobby, without flashing the bar', async () => {
      const openOnLaunch = vi.fn()
      signIn()
      const rejoin = mount({ openOnLaunch })
      await flush()

      expect(openOnLaunch).toHaveBeenCalledWith(expect.objectContaining({ joinCode: 'ABCD' }))
      expect(rejoin.isVisible.value).toBe(false)
    })

    it('lets the player choose between several', async () => {
      vi.mocked(LobbyService.getActiveLobbies).mockResolvedValue([lobby('ABCD'), lobby('EFGH')])
      const openOnLaunch = vi.fn()
      signIn()
      const rejoin = mount({ openOnLaunch })
      await flush()

      expect(openOnLaunch).not.toHaveBeenCalled()
      expect(rejoin.isVisible.value).toBe(true)
    })

    it('leaves it alone once the bar was dismissed for it', async () => {
      signIn()
      const earlier = mount()
      await flush()
      earlier.dismiss()

      const openOnLaunch = vi.fn()
      mount({ openOnLaunch })
      await flush()
      expect(openOnLaunch).not.toHaveBeenCalled()
    })

    it('stays on the page when the player came to it from another one', async () => {
      recordNavigation({ name: 'Lobby', path: '/lobby/ABCD' } as RouteLocationNormalized)
      const openOnLaunch = vi.fn()
      signIn()
      const rejoin = mount({ openOnLaunch })
      await flush()

      expect(openOnLaunch).not.toHaveBeenCalled()
      expect(rejoin.isVisible.value).toBe(true)
    })

    it('stays put when the player moved on before the lobbies loaded', async () => {
      const openOnLaunch = vi.fn()
      signIn()
      mount({ openOnLaunch })
      recordNavigation({ name: 'Home', path: '/' } as RouteLocationNormalized)
      await flush()

      expect(openOnLaunch).not.toHaveBeenCalled()
    })
  })
})
