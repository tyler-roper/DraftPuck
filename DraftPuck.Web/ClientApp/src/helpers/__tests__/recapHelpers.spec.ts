import { describe, expect, it } from 'vitest'
import {
  awardStyle,
  buildRecapCard,
  drinksWord,
  MAX_CARD_STANDINGS,
  memberAvatar,
  recapFileName,
  recapStandings,
  recapTotals,
  standingSubtitle
} from '@/helpers/recapHelpers'
import BotPickStyle from '@/enums/botPickStyle'

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

describe('the standings and totals', () => {
  const drink = (to?: string) => ({ recipientLobbyMemberId: to }) as Drink
  const pick = (drinks: Drink[], isActive = true) => ({ isActive, drinks }) as LobbyMemberPick
  const member = (id: string, picks: LobbyMemberPick[]) => ({ id, picks }) as unknown as LobbyMember
  // Sam gave 1 (and earned 1 not handed out yet); Max gave 2, one from a pick later removed; Jo gave 1.
  const lobby = {
    gameIds: [1, 2],
    members: [
      member('sam', [pick([drink('max'), drink()])]),
      member('max', [pick([drink('sam')]), pick([drink('jo')], false)]),
      member('jo', [pick([drink('sam')])])
    ]
  } as unknown as Lobby

  it('ranks members by drinks given from active picks, counting every drink taken', () => {
    expect(recapStandings(lobby).map((s) => [s.member.id, s.given, s.taken])).toEqual([
      ['sam', 1, 2],
      ['max', 1, 1],
      ['jo', 1, 1]
    ])
  })

  it("totals the lobby's games, players, active picks and drinks handed out from them", () => {
    expect(recapTotals(lobby)).toEqual({ games: 2, players: 3, picks: 3, drinks: 3 })
  })
})

describe('standingSubtitle', () => {
  const member = (extra: Partial<LobbyMember>) => ({ userId: 'u1', isBot: false, isGuest: false, ...extra }) as LobbyMember

  it("is a bot's strategy", () => {
    expect(standingSubtitle(member({ isBot: true, botPickStyle: BotPickStyle.Worst }), 'me')).toBe('Strategy: Worst')
  })

  it('names guests, and the viewer as "You (Guest)"', () => {
    expect(standingSubtitle(member({ isGuest: true }), 'me')).toBe('Guest')
    expect(standingSubtitle(member({ isGuest: true }), 'u1')).toBe('You (Guest)')
  })

  it("is an account's title", () => {
    expect(standingSubtitle(member({ title: { text: 'Sniper' } as Title }), 'me')).toBe('Sniper')
  })
})
