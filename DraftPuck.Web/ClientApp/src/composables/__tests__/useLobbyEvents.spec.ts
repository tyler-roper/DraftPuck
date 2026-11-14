import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
import { useUserStore } from '@/stores/user'
import { useLobbyEvents, type LobbyEventReactions } from '@/composables/useLobbyEvents'
import { useLobbyChat } from '@/composables/useLobbyChat'
import LobbyEventType from '@/enums/lobbyEventType'

function setup() {
  setActivePinia(createPinia())
  useUserStore().currentUser = { id: 'u1' } as User
  const store = useLobbyStore()
  store.setLobby({
    joinCode: 'ABCD',
    members: [
      { id: 'me', userId: 'u1', name: 'Mc David', picks: [], messages: [] },
      { id: 'other', userId: 'u2', name: 'Alex', picks: [], messages: [] }
    ]
  } as unknown as Lobby)
  return store
}

const event = (lobbyEventType: LobbyEventType, extra: Partial<LobbyEvent> = {}) =>
  ({ id: 'e1', lobbyEventType, timeUtc: '2026-11-14T01:00:00Z', ...extra }) as unknown as LobbyEvent

describe('useLobbyEvents', () => {
  let reactions: LobbyEventReactions

  beforeEach(() => {
    reactions = { refreshLobby: vi.fn(async () => {}), showDrinkForMe: vi.fn(), notify: vi.fn(), onRemovedFromLobby: vi.fn() }
  })

  it('refreshes the lobby and adds the event to the feed, with its dates parsed', async () => {
    const store = setup()

    await useLobbyEvents(reactions).onLobbyEvent(event(LobbyEventType.NewPick, { lobbyMemberId: 'me' }))

    expect(reactions.refreshLobby).toHaveBeenCalledOnce()
    expect(store.events).toHaveLength(1)
    expect(store.events[0].timeUtc).toBeInstanceOf(Date)
  })

  it('sends you away when you are removed or leave, without touching the lobby', async () => {
    const store = setup()
    const { onLobbyEvent } = useLobbyEvents(reactions)

    await onLobbyEvent(event(LobbyEventType.UserRemoved, { lobbyMemberId: 'me' }))
    await onLobbyEvent(event(LobbyEventType.UserLeft, { lobbyMemberId: 'me' }))

    expect(vi.mocked(reactions.onRemovedFromLobby).mock.calls).toEqual([['You were removed from the lobby.'], ['You left the lobby.']])
    expect(reactions.refreshLobby).not.toHaveBeenCalled()
    expect(store.events).toEqual([])
  })

  it("treats someone else's removal as a normal event", async () => {
    setup()

    await useLobbyEvents(reactions).onLobbyEvent(event(LobbyEventType.UserRemoved, { lobbyMemberId: 'other' }))

    expect(reactions.onRemovedFromLobby).not.toHaveBeenCalled()
    expect(reactions.refreshLobby).toHaveBeenCalledOnce()
  })

  it('shows drinks given to you as the animation, and drinks between others as a toast', async () => {
    setup()
    const { onLobbyEvent } = useLobbyEvents(reactions)

    await onLobbyEvent(event(LobbyEventType.DrinkAssigned, { lobbyMemberId: 'other', lobbyMember2Id: 'me' }))
    await onLobbyEvent(event(LobbyEventType.DrinkAssigned, { lobbyMemberId: 'me', lobbyMember2Id: 'other' }))

    expect(reactions.showDrinkForMe).toHaveBeenCalledOnce()
    expect(reactions.notify).toHaveBeenCalledOnce()
  })

  it('celebrates your own awarded drinks and just informs about others', async () => {
    setup()
    const { onLobbyEvent } = useLobbyEvents(reactions)

    await onLobbyEvent(event(LobbyEventType.DrinkAwarded, { lobbyMemberId: 'me' }))
    await onLobbyEvent(event(LobbyEventType.DrinkAwarded, { lobbyMemberId: 'other' }))
    await onLobbyEvent(event(LobbyEventType.UserJoined, { lobbyMemberId: 'other' }))

    expect(vi.mocked(reactions.notify).mock.calls.map(([, tone]) => tone)).toEqual(['success', 'info', undefined])
  })
})

describe('useLobbyChat', () => {
  const message = (lobbyMemberId: string, text: string) => ({ lobbyMemberId, message: text, sent: '2026-11-14T01:00:00Z' }) as unknown as Message

  it("adds others' messages and counts unread ones and mentions while the chat is closed", () => {
    const store = setup()
    let chatOpen = false
    const chat = useLobbyChat(() => chatOpen)

    chat.onMessage(message('other', 'hi all'))
    chat.onMessage(message('other', 'hey @mc david'))
    chatOpen = true
    chat.onMessage(message('other', 'seen this one'))

    expect(store.chatMessages).toHaveLength(3)
    expect(chat.unreadCount.value).toBe(2)
    expect(chat.unreadMentionCount.value).toBe(1)
  })

  it('ignores your own messages, which are already shown when sent', () => {
    const store = setup()
    const chat = useLobbyChat(() => false)

    chat.onMessage(message('me', 'mine'))

    expect(store.chatMessages).toEqual([])
    expect(chat.unreadCount.value).toBe(0)
  })

  it('clears both counts when the chat is read', () => {
    setup()
    const chat = useLobbyChat(() => false)
    chat.onMessage(message('other', '@Mc David look'))

    chat.markRead()

    expect([chat.unreadCount.value, chat.unreadMentionCount.value]).toEqual([0, 0])
  })
})
