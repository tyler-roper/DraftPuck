import { describe, expect, it } from 'vitest'
import { awardStyle, buildRecapCard, drinksWord, MAX_CARD_STANDINGS, memberAvatar, recapFileName } from '@/helpers/recapHelpers'

const award = (key: string, memberName = 'Sam'): LobbyAward => ({ key, title: key, memberId: 'id', memberName, detail: 'did a thing' })

describe('awardStyle', () => {
  it('has an icon and colour for each award, and a star for unknown ones', () => {
    expect(awardStyle('best_pick').icon).toBe('trophy')
    expect(awardStyle('ice_cold').icon).toBe('snowflake')
    expect(awardStyle('something_new').icon).toBe('star')
    expect([awardStyle('best_pick').motion, awardStyle('ice_cold').motion, awardStyle('something_new').motion]).toEqual(['bob', 'shiver', 'twinkle'])
  })
})

describe('buildRecapCard', () => {
  const standings = ['Sam', 'Alex', 'Jo', 'Pat'].map((name, i) => ({ name, given: 10 - i, taken: i }))

  it('carries the lobby, its totals, the awards and the top of the standings', () => {
    const card = buildRecapCard(
      'ABCD',
      'Yesterday',
      { games: 3, players: 4, picks: 12, drinks: 9 },
      [award('best_pick'), award('ice_cold', 'Jo')],
      standings
    )

    expect(card.heading).toBe('Lobby ABCD')
    expect(card.subheading).toBe('Yesterday')
    expect(card.totals).toEqual([
      { label: 'Games', value: 3 },
      { label: 'Players', value: 4 },
      { label: 'Picks', value: 12 },
      { label: 'Drinks', value: 9 }
    ])
    expect(card.awards.map((a) => [a.winner, a.color])).toEqual([
      ['Sam', '#ffb100'],
      ['Jo', '#00ccff']
    ])
    expect(card.standings.map((s) => s.name)).toEqual(standings.slice(0, MAX_CARD_STANDINGS).map((s) => s.name))
  })
})

describe('recapFileName', () => {
  it('names the image after the lobby', () => {
    expect(recapFileName('ABCD')).toBe('draftpuck-abcd-recap.png')
  })
})

describe('memberAvatar', () => {
  it("shows an account's picture", () => {
    expect(memberAvatar({ name: 'sam', avatarPath: '/a.png', isGuest: false })).toEqual({ style: { backgroundImage: 'url(/a.png)' }, initial: '' })
  })

  it('shows the initial for guests and anyone without a picture', () => {
    expect(memberAvatar({ name: 'sam', avatarPath: '/a.png', isGuest: true })).toEqual({ style: {}, initial: 'S' })
    expect(memberAvatar({ name: 'jo', avatarPath: undefined, isGuest: false } as unknown as LobbyMember)).toEqual({ style: {}, initial: 'J' })
  })
})

describe('drinksWord', () => {
  it('is singular for one', () => {
    expect([0, 1, 2].map(drinksWord)).toEqual(['drinks', 'drink', 'drinks'])
  })
})
