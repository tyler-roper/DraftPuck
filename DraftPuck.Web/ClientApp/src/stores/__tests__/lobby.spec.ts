import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
import LobbyService from '@/services/LobbyService'
import PickRequest from '@/models/pickRequest'
import { useUserStore } from '@/stores/user'
import type Bot from '@/models/bot'

vi.mock('@/services/LobbyService', () => ({
  default: { makePick: vi.fn(), removePick: vi.fn(), changeName: vi.fn(), removeLobbyMember: vi.fn(), addBot: vi.fn(), sendMessage: vi.fn(), getLobbyByCode: vi.fn() }
}))

const refusal = (detail: string) => Object.assign(new Error('400'), { response: { data: { detail } } })

function setupLobby() {
  const store = useLobbyStore()
  store.setLobby({
    joinCode: 'ABCD',
    members: [{ id: 'me', picks: [{ id: 'p0', lobbyMemberId: 'me', gameId: 10, teamId: 1, playerId: 100 }] }]
  } as unknown as Lobby)
  return store
}

const myPicks = (store: ReturnType<typeof useLobbyStore>) => store.lobby!.members[0].picks.map((p) => p.id)
const persisted = (id: string, playerId: number) => ({ id, lobbyMemberId: 'me', gameId: 10, teamId: 1, playerId }) as LobbyMemberPick

describe('lobby store picks', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(LobbyService.makePick).mockReset()
    vi.mocked(LobbyService.removePick).mockReset()
  })

  it('shows nothing until the server confirms, then adds the pick the server saved', async () => {
    const store = setupLobby()
    let confirm!: (pick: LobbyMemberPick) => void
    vi.mocked(LobbyService.makePick).mockReturnValue(new Promise((resolve) => (confirm = resolve)))

    const result = store.makePicks([new PickRequest('me', 101, 10, 1)])
    expect(myPicks(store)).toEqual(['p0'])

    confirm(persisted('p1', 101))
    expect(await result).toEqual([{ request: expect.any(PickRequest) }])
    expect(myPicks(store)).toEqual(['p0', 'p1'])
  })

  it('never shows a refused pick, and returns the server reason for it', async () => {
    const store = setupLobby()
    vi.mocked(LobbyService.makePick).mockRejectedValue(refusal('Player already picked.'))

    const [result] = await store.makePicks([new PickRequest('me', 101, 10, 1)])

    expect(result.error).toBe('Player already picked.')
    expect(myPicks(store)).toEqual(['p0'])
  })

  it('handles each pick on its own, so one refusal does not undo the others', async () => {
    const store = setupLobby()
    vi.mocked(LobbyService.makePick)
      .mockResolvedValueOnce(persisted('p1', 101))
      .mockRejectedValueOnce(refusal('No picks remaining for this team.'))

    const results = await store.makePicks([new PickRequest('me', 101, 10, 1), new PickRequest('me', 102, 10, 1)])

    expect(results.map((r) => r.error)).toEqual([undefined, 'No picks remaining for this team.'])
    expect(myPicks(store)).toEqual(['p0', 'p1'])
  })

  it('does not add a pick twice when a lobby refetch already brought it in', async () => {
    const store = setupLobby()
    vi.mocked(LobbyService.makePick).mockResolvedValue(persisted('p0', 100))

    await store.makePicks([new PickRequest('me', 100, 10, 1)])

    expect(myPicks(store)).toEqual(['p0'])
  })

  it('removes a pick only after the server has, and keeps it when the server refuses', async () => {
    const store = setupLobby()
    vi.mocked(LobbyService.removePick).mockRejectedValueOnce(refusal("Picks can't be removed after the game has started."))

    await expect(store.removePick('p0')).rejects.toThrow()
    expect(myPicks(store)).toEqual(['p0'])

    vi.mocked(LobbyService.removePick).mockResolvedValueOnce()
    await store.removePick('p0')
    expect(myPicks(store)).toEqual([])
  })
})

describe('lobby store changes', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  function setupMembers() {
    useUserStore().currentUser = { id: 'u1' } as User
    const store = useLobbyStore()
    store.setLobby({
      joinCode: 'ABCD',
      members: [
        { id: 'me', userId: 'u1', name: 'Sam', picks: [], messages: [] },
        { id: 'other', userId: 'u2', name: 'Alex', picks: [], messages: [] }
      ]
    } as unknown as Lobby)
    return store
  }

  it('renames only after the server accepts, and keeps the old name when it refuses', async () => {
    const store = setupMembers()
    vi.mocked(LobbyService.changeName).mockRejectedValueOnce(refusal('Name already taken.'))

    await expect(store.changeName('Alex')).rejects.toThrow()
    expect(store.currentMember?.name).toBe('Sam')

    vi.mocked(LobbyService.changeName).mockResolvedValueOnce()
    await store.changeName('Samuel')
    expect(store.currentMember?.name).toBe('Samuel')
  })

  it('removes a member only after the server has', async () => {
    const store = setupMembers()
    vi.mocked(LobbyService.removeLobbyMember).mockRejectedValueOnce(refusal('Forbidden'))

    await expect(store.removeLobbyMember('other')).rejects.toThrow()
    expect(store.lobby!.members).toHaveLength(2)

    vi.mocked(LobbyService.removeLobbyMember).mockResolvedValueOnce()
    await store.removeLobbyMember('other')
    expect(store.lobby!.members.map((m) => m.id)).toEqual(['me'])
  })

  it('adds a bot by refetching the lobby the server updated', async () => {
    const store = setupMembers()
    const withBot = { joinCode: 'ABCD', members: [{ id: 'me' }, { id: 'other' }, { id: 'bot', isBot: true }] } as unknown as Lobby
    vi.mocked(LobbyService.addBot).mockResolvedValueOnce(undefined)
    vi.mocked(LobbyService.getLobbyByCode).mockResolvedValueOnce(withBot)

    await store.addBot({ name: 'Botty' } as Bot)

    expect(store.lobby!.members.map((m) => m.id)).toContain('bot')
  })

  it('shows a chat message immediately, and takes it back if sending fails', async () => {
    const store = setupMembers()
    let fail!: (error: unknown) => void
    vi.mocked(LobbyService.sendMessage).mockReturnValueOnce(new Promise((_, reject) => (fail = reject)))

    const sending = store.sendMessage('hello')
    expect(store.chatMessages.map((m) => m.message)).toEqual(['hello'])

    fail(new Error('offline'))
    await expect(sending).rejects.toThrow('offline')
    expect(store.chatMessages).toEqual([])
  })

  it('lists every member message and local system message, oldest first', () => {
    const store = setupMembers()
    store.lobby!.members[0].messages.push({ lobbyMemberId: 'me', message: 'second', sent: new Date(2026, 0, 1, 12, 2) } as Message)
    store.lobby!.members[1].messages.push({ lobbyMemberId: 'other', message: 'first', sent: new Date(2026, 0, 1, 12, 1) } as Message)

    expect(store.chatMessages.map((m) => m.message)).toEqual(['first', 'second'])
  })
})
