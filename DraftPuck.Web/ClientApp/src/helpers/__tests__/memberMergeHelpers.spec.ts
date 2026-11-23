import { describe, expect, it } from 'vitest'
import { describeSpotContents, mergeCandidates, spotContents } from '@/helpers/memberMergeHelpers'

function member(id: string, overrides: Partial<LobbyMember> = {}): LobbyMember {
  return { id, userId: `user-${id}`, name: id, picks: [], messages: [], isBot: false, isRemoved: false, isGuest: true, ...overrides } as LobbyMember
}

function lobbyOf(members: LobbyMember[], createdBy = 'user-host'): Lobby {
  return { members, createdBy } as Lobby
}

describe('mergeCandidates', () => {
  it('offers the other guests still in the lobby', () => {
    const duplicate = member('sam2')
    const lobby = lobbyOf([
      member('host'),
      member('sam'),
      duplicate,
      member('robo', { isBot: true }),
      member('casey', { isGuest: false }),
      member('gone', { isRemoved: true })
    ])

    expect(mergeCandidates(lobby, duplicate).map((m) => m.id)).toEqual(['sam'])
  })

  it('offers nothing for a bot', () => {
    const bot = member('robo', { isBot: true })

    expect(mergeCandidates(lobbyOf([member('sam'), bot]), bot)).toEqual([])
  })
})

describe('spotContents', () => {
  it('counts picks, the drinks they earned and the drinks given to the spot', () => {
    const sam = member('sam', {
      picks: [{ drinks: [{ recipientLobbyMemberId: 'casey' }, {}] }, { drinks: [] }] as LobbyMemberPick[]
    })
    const casey = member('casey', { picks: [{ drinks: [{ recipientLobbyMemberId: 'sam' }] }] as LobbyMemberPick[] })

    expect(spotContents(lobbyOf([sam, casey]), sam)).toEqual({ picks: 2, drinksEarned: 2, drinksTaken: 1 })
  })
})

describe('describeSpotContents', () => {
  it('lists what the spot holds', () => {
    expect(describeSpotContents({ picks: 2, drinksEarned: 3, drinksTaken: 1 })).toBe('2 picks, 3 drinks earned and 1 drink taken')
    expect(describeSpotContents({ picks: 1, drinksEarned: 0, drinksTaken: 2 })).toBe('1 pick and 2 drinks taken')
    expect(describeSpotContents({ picks: 0, drinksEarned: 1, drinksTaken: 0 })).toBe('1 drink earned')
  })

  it('says when a spot has nothing', () => {
    expect(describeSpotContents({ picks: 0, drinksEarned: 0, drinksTaken: 0 })).toBe('nothing yet')
  })
})
