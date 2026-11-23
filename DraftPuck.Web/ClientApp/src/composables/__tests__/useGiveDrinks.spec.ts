import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useLobbyStore } from '@/stores/lobby'
import { useUserStore } from '@/stores/user'
import { useGiveDrinks } from '@/composables/useGiveDrinks'
import LobbyService from '@/services/LobbyService'

function setup(joinCode = 'ABCD') {
  setActivePinia(createPinia())
  useUserStore().currentUser = { id: 'u1' } as User
  const store = useLobbyStore()
  store.setLobby({
    joinCode,
    members: [
      { id: 'me', userId: 'u1', name: 'Mc David', messages: [], picks: [{ id: 'p1', drinks: [{ id: 'd1' }, { id: 'd2' }] }] },
      { id: 'other', userId: 'u2', name: 'Alex', messages: [], picks: [] },
      { id: 'bot', userId: 'u3', name: 'Bot', isBot: true, messages: [], picks: [] }
    ]
  } as unknown as Lobby)
  return store
}

/** A goal for one of my picks: a new drink to give. */
function earnDrink(store: ReturnType<typeof useLobbyStore>, id: string) {
  store.lobby!.members[0].picks[0].drinks.push({ id } as Drink)
}

describe('useGiveDrinks', () => {
  beforeEach(() => {
    localStorage.clear()
    // The sheet is shared across the page, so it carries over between tests: close it.
    setActivePinia(createPinia())
    useGiveDrinks().closeSheet()
    vi.restoreAllMocks()
  })

  it('lists my drinks to give and who can take one', () => {
    setup()
    const { pendingDrinks, recipients } = useGiveDrinks()

    expect(pendingDrinks.value.map((d) => d.id)).toEqual(['d1', 'd2'])
    expect(recipients.value.map((m) => m.id)).toEqual(['other'])
  })

  it('shrinks the bar when dismissed, and brings it back for a new drink', () => {
    const store = setup()
    const { isCollapsed, collapse } = useGiveDrinks()
    expect(isCollapsed.value).toBe(false)

    collapse()
    expect(isCollapsed.value).toBe(true)

    earnDrink(store, 'd3')
    expect(isCollapsed.value).toBe(false)
  })

  it('remembers the dismissal across page loads', () => {
    localStorage.setItem('draftpuck.giveBarDismissed.WXYZ', JSON.stringify(['d1', 'd2']))
    setup('WXYZ')

    expect(useGiveDrinks().isCollapsed.value).toBe(true)
  })

  it('shares the sheet between callers, and closes it once the last drink is given', async () => {
    const store = setup()
    vi.spyOn(LobbyService, 'assignDrink').mockImplementation(
      async (_code, drinkId, recipientId) => ({ id: drinkId, recipientLobbyMemberId: recipientId }) as Drink
    )
    const bar = useGiveDrinks()
    const card = useGiveDrinks()
    const alex = store.lobby!.members[1]

    card.openSheet()
    expect(bar.isSheetOpen.value).toBe(true)

    await bar.give(alex, 'Cheers')
    expect(LobbyService.assignDrink).toHaveBeenLastCalledWith('ABCD', 'd1', 'other', 'Cheers')
    expect(bar.isSheetOpen.value).toBe(true)

    await bar.give(alex)
    expect(LobbyService.assignDrink).toHaveBeenLastCalledWith('ABCD', 'd2', 'other', undefined)
    expect(bar.isSheetOpen.value).toBe(false)
  })

  it("doesn't open the sheet with nothing to give", () => {
    setup()
    useLobbyStore().lobby!.members[0].picks = []
    const { isSheetOpen, openSheet } = useGiveDrinks()

    openSheet()

    expect(isSheetOpen.value).toBe(false)
  })
})
