import { describe, expect, it } from 'vitest'
import { drinkRecipients, drinksToGive, isGiveBarCollapsed } from '@/helpers/giveDrinkHelpers'

const member = (id: string, overrides: Partial<LobbyMember> = {}) =>
  ({ id, userId: `user-${id}`, name: id, picks: [], messages: [], isBot: false, isRemoved: false, ...overrides }) as LobbyMember

describe('drinksToGive', () => {
  it("lists the drinks the member's picks earned that nobody has been given", () => {
    const me = member('me', {
      picks: [{ drinks: [{ id: 'given', recipientLobbyMemberId: 'casey' }, { id: 'a' }] }, { drinks: [{ id: 'b' }] }] as LobbyMemberPick[]
    })

    expect(drinksToGive(me).map((d) => d.id)).toEqual(['a', 'b'])
  })

  it('has none without a member', () => {
    expect(drinksToGive(undefined)).toEqual([])
  })
})

describe('drinkRecipients', () => {
  it('offers everyone but you and the bots', () => {
    const lobby = { members: [member('me'), member('casey'), member('robo', { isBot: true }), member('alex')] } as Lobby

    expect(drinkRecipients(lobby, 'me').map((m) => m.id)).toEqual(['casey', 'alex'])
  })
})

describe('isGiveBarCollapsed', () => {
  it('stays shrunk for the drinks there when it was dismissed', () => {
    expect(isGiveBarCollapsed(['a', 'b'], ['a', 'b'])).toBe(true)
    // Giving one out doesn't bring the full bar back.
    expect(isGiveBarCollapsed(['b'], ['a', 'b'])).toBe(true)
  })

  it('comes back in full for a new drink', () => {
    expect(isGiveBarCollapsed(['a', 'c'], ['a', 'b'])).toBe(false)
  })

  it('is full until dismissed', () => {
    expect(isGiveBarCollapsed(['a'], [])).toBe(false)
  })
})
