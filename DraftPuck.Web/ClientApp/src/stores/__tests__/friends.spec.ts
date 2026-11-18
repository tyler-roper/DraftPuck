import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useFriendsStore } from '@/stores/friends'
import FriendService from '@/services/FriendService'

vi.mock('@/services/FriendService', () => ({
  default: { getFriends: vi.fn(), addFriend: vi.fn(), removeFriend: vi.fn(), dismissLobbyInvite: vi.fn() }
}))

const friend = (id: string): Friend => ({ id, nickname: id })
const invite = (joinCode: string): LobbyInvite => ({ joinCode, sender: friend('host'), gameCount: 1, memberCount: 2, created: new Date() })
const list = (overrides: Partial<FriendsList> = {}): FriendsList => ({ friends: [], incoming: [], outgoing: [], invites: [], suggestions: [], ...overrides })

describe('friends store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('counts what is waiting on the user once loaded', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list({ incoming: [friend('a')], invites: [invite('ABCD')] }))
    const store = useFriendsStore()
    expect(store.pending).toBe(0)

    await store.load()

    expect(store.pending).toBe(2)
  })

  it('shares one fetch between calls made while it runs', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list())
    const store = useFriendsStore()

    await Promise.all([store.load(), store.load()])
    await store.load()

    expect(FriendService.getFriends).toHaveBeenCalledTimes(2)
  })

  it('reloads after adding or removing a friend', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list({ friends: [friend('a')] }))
    const store = useFriendsStore()

    await store.add('a')
    expect(FriendService.addFriend).toHaveBeenCalledWith('a')
    expect(store.list?.friends).toHaveLength(1)

    vi.mocked(FriendService.getFriends).mockResolvedValue(list())
    await store.remove('a')
    expect(FriendService.removeFriend).toHaveBeenCalledWith('a')
    expect(store.list?.friends).toHaveLength(0)
  })

  it('drops a dismissed invite straight away', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list({ invites: [invite('ABCD'), invite('EFGH')] }))
    const store = useFriendsStore()
    await store.load()

    await store.dismissInvite('ABCD')

    expect(FriendService.dismissLobbyInvite).toHaveBeenCalledWith('ABCD')
    expect(store.list?.invites.map((i) => i.joinCode)).toEqual(['EFGH'])
    expect(store.pending).toBe(1)
  })

  it('drops the invite to a lobby the user is now in, whatever the case of the code', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list({ incoming: [friend('a')], invites: [invite('ABCD'), invite('EFGH')] }))
    const store = useFriendsStore()
    await store.load()

    store.forgetInvite('abcd')
    store.forgetInvite('ZZZZ')

    expect(store.list?.invites.map((i) => i.joinCode)).toEqual(['EFGH'])
    expect(store.pending).toBe(2)
    expect(FriendService.dismissLobbyInvite).not.toHaveBeenCalled()
  })

  it('forgets everything on reset (sign-out)', async () => {
    vi.mocked(FriendService.getFriends).mockResolvedValue(list({ incoming: [friend('a')] }))
    const store = useFriendsStore()
    await store.load()

    store.reset()

    expect(store.list).toBeUndefined()
    expect(store.pending).toBe(0)
  })
})
