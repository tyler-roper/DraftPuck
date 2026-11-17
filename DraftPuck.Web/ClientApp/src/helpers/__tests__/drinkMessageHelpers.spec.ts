import { describe, expect, it } from 'vitest'
import { DRINK_MESSAGE_MAX_LENGTH, DRINK_MESSAGE_SUGGESTIONS, messageForDrinkEvent, tidyDrinkMessage } from '@/helpers/drinkMessageHelpers'

describe('tidyDrinkMessage', () => {
  it('collapses whitespace, and drops a message with nothing in it', () => {
    expect(tidyDrinkMessage('  That is\n  for   you ')).toBe('That is for you')
    expect(tidyDrinkMessage('   ')).toBeUndefined()
  })

  it('offers suggestions that fit', () => {
    for (const s of DRINK_MESSAGE_SUGGESTIONS) expect(s.length).toBeLessThanOrEqual(DRINK_MESSAGE_MAX_LENGTH)
  })
})

describe('messageForDrinkEvent', () => {
  const drink = (eventId: number, to: string, message?: string) => ({ eventId, recipientLobbyMemberId: to, message })
  const lobby = {
    members: [
      { id: 'sam', picks: [{ drinks: [drink(7, 'jo', 'Revenge.'), drink(8, 'jo')] }] },
      { id: 'jo', picks: [{ drinks: [drink(7, 'sam', 'Not this one')] }] }
    ]
  } as unknown as Lobby
  const handout = (from: string, to: string, gameEventId: number) =>
    ({ lobbyMemberId: from, lobbyMember2Id: to, gameEventId }) as unknown as LobbyEvent

  it("finds the sender's drink for that goal and recipient", () => {
    expect(messageForDrinkEvent(lobby, handout('sam', 'jo', 7))).toBe('Revenge.')
  })

  it('is undefined when the drink came without a message, or is not found', () => {
    expect(messageForDrinkEvent(lobby, handout('sam', 'jo', 8))).toBeUndefined()
    expect(messageForDrinkEvent(lobby, handout('sam', 'jo', 99))).toBeUndefined()
    expect(messageForDrinkEvent(lobby, handout('nobody', 'jo', 7))).toBeUndefined()
  })
})
