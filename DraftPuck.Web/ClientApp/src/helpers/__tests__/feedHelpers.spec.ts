import { describe, expect, it } from 'vitest'
import { buildFeedItems, chainKeys, goalDrinks, goalKeyFor } from '@/helpers/feedHelpers'
import FeedItemType from '@/enums/feedItemType'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'

const lobbyCreated = new Date('2026-11-13T23:00:00Z')
const at = (minute: number) => new Date(Date.UTC(2026, 10, 13, 23, minute))

function play(id: number, type: PlayType, dateTime: Date, extra: Partial<Play> = {}): Play {
  return {
    id,
    type,
    dateTime,
    period: 1,
    periodType: PeriodType.Regulation,
    timeInPeriod: '05:00',
    timeRemainingInPeriod: '15:00',
    primaryPlayerId: null,
    primaryTeamId: null,
    homeScore: 0,
    awayScore: 0,
    penalty: null,
    ...extra
  }
}

function game(id: number, plays: Play[]): Game {
  return {
    id,
    plays,
    homeTeam: { id: 1, abbreviation: 'EDM', score: 0 },
    awayTeam: { id: 2, abbreviation: 'CGY', score: 0 },
    playerSummaries: []
  } as unknown as Game
}

function lobbyEvent(id: string, timeUtc: Date, extra: Partial<LobbyEvent> = {}): LobbyEvent {
  return { id, timeUtc, title: 'Event', text: 'text', lobbyEventType: 1, ...extra } as unknown as LobbyEvent
}

describe('buildFeedItems', () => {
  it('includes notable plays and leaves out the rest', () => {
    const plays = [
      play(1, PlayType.PeriodStart, at(1)),
      play(2, PlayType.Goal, at(2)),
      play(3, PlayType.Penalty, at(3)),
      play(4, PlayType.Challenge, at(4)),
      play(5, PlayType.PeriodEnd, at(5)),
      play(6, PlayType.GameEnd, at(6)),
      play(7, PlayType.Faceoff, at(7)),
      play(8, PlayType.Hit, at(8))
    ]

    const items = buildFeedItems([game(10, plays)], [], lobbyCreated, false)

    expect(items.map((i) => i.subType)).toEqual([
      PlayType.PeriodStart,
      PlayType.Goal,
      PlayType.Penalty,
      PlayType.Challenge,
      PlayType.PeriodEnd,
      PlayType.GameEnd
    ])
    expect(items.every((i) => i.type === FeedItemType.GameEvent && i.gameId === 10)).toBe(true)
  })

  it('leaves out plays from before the lobby was created, except in test mode', () => {
    const early = play(1, PlayType.Goal, new Date('2026-11-13T22:59:00Z'))
    const late = play(2, PlayType.Goal, at(1))

    expect(buildFeedItems([game(10, [early, late])], [], lobbyCreated, false)).toHaveLength(1)
    expect(buildFeedItems([game(10, [early, late])], [], lobbyCreated, true)).toHaveLength(2)
  })

  // The feed animates new items by key, so keys must survive refetches and never collide across games.
  it('keys each item by its game and play, or its lobby event', () => {
    const items = buildFeedItems(
      [game(10, [play(1, PlayType.Goal, at(1))]), game(20, [play(1, PlayType.Goal, at(2))])],
      [lobbyEvent('e1', at(3))],
      lobbyCreated,
      false
    )

    expect(items.map((i) => i.key)).toEqual(['play-10-1', 'play-20-1', 'event-e1'])
  })

  it('lays out each kind of item: goals loudest, penalties quietly, periods and finals with both teams, lobby events apart', () => {
    const plays = [
      play(1, PlayType.Goal, at(1), { primaryTeamId: 2, homeScore: 0, awayScore: 1 }),
      play(2, PlayType.Penalty, at(2), { primaryTeamId: 1 }),
      play(3, PlayType.PeriodEnd, at(3)),
      play(4, PlayType.GameEnd, at(4))
    ]

    const items = buildFeedItems([game(10, plays)], [lobbyEvent('e1', at(5))], lobbyCreated, false)

    expect(items.map((i) => i.layout)).toEqual(['goal', 'game', 'state', 'final', 'lobby'])
    expect(items[0].teams.map((t) => t.abbreviation)).toEqual(['CGY'])
    expect(items[2].teams.map((t) => t.abbreviation)).toEqual(['CGY', 'EDM'])
    expect(items[4].lobbyEvent?.id).toBe('e1')
  })

  it('titles a goal with the scoring team and its score first, and gives a period its score', () => {
    const players = [{ id: 97, teamId: 1, firstName: 'Connor', lastName: 'McDavid' }] as PlayerSummary[]
    const edmGoal = play(1, PlayType.Goal, at(1), { primaryTeamId: 1, primaryPlayerId: 97, homeScore: 2, awayScore: 1, period: 3, timeInPeriod: '12:41' })
    const cgyGoal = play(2, PlayType.Goal, at(2), { primaryTeamId: 2, homeScore: 2, awayScore: 2 })
    const periodEnd = play(3, PlayType.PeriodEnd, at(3), { period: 2, homeScore: 1, awayScore: 0 })

    const [edm, cgy, period] = buildFeedItems([{ ...game(10, [edmGoal, cgyGoal, periodEnd]), playerSummaries: players }], [], lobbyCreated, false)

    expect(edm.title).toBe('Goal · EDM 2–1')
    expect(edm.text).toContain('<strong>Connor McDavid</strong>')
    expect(edm.detail).toBe('3rd 12:41')
    expect(cgy.title).toBe('Goal · CGY 2–2')
    expect(cgy.text).toBe('Scorer not yet assigned…')
    expect([period.title, period.text]).toEqual(['End of 2nd', '<strong>CGY 0–1 EDM</strong>'])
  })

  it("names a final's winner first, and anyone who scored more than once", () => {
    const players = [{ id: 97, teamId: 1, lastName: 'McDavid' }, { id: 29, teamId: 1, lastName: 'Draisaitl' }] as PlayerSummary[]
    const goal = (id: number, scorer: number) => play(id, PlayType.Goal, at(id), { primaryTeamId: 1, primaryPlayerId: scorer })
    const finished = { ...game(10, [goal(1, 97), goal(2, 29), goal(3, 97), play(4, PlayType.GameEnd, at(4))]), playerSummaries: players }
    finished.homeTeam.score = 3

    const final = buildFeedItems([finished], [], lobbyCreated, false).at(-1)!

    expect(final.text).toBe('EDM 3 <span class="loser">– 0 CGY</span>')
    expect(final.detail).toBe('McDavid 2G')
  })

  it('leaves out shootout goals', () => {
    const shootoutGoal = play(1, PlayType.Goal, at(1), { periodType: PeriodType.Shootout })

    expect(buildFeedItems([game(10, [shootoutGoal])], [], lobbyCreated, false)).toHaveLength(0)
  })

  it('merges plays from every game with lobby events, oldest first', () => {
    const items = buildFeedItems(
      [game(10, [play(1, PlayType.Goal, at(3))]), game(20, [play(2, PlayType.Goal, at(1))])],
      [lobbyEvent('e1', at(2)), lobbyEvent('e2', at(4))],
      lobbyCreated,
      false
    )

    expect(items.map((i) => i.time)).toEqual([at(1), at(2), at(3), at(4)])
    expect(items.map((i) => i.type)).toEqual([FeedItemType.GameEvent, FeedItemType.LobbyEvent, FeedItemType.GameEvent, FeedItemType.LobbyEvent])
  })
})

describe('links between a goal and its drinks', () => {
  const goal = play(6, PlayType.Goal, at(10), { primaryTeamId: 1 })
  const drinkEvent = (id: string, type: number, extra: Partial<LobbyEvent> = {}) => lobbyEvent(id, at(12), { lobbyEventType: type, gameId: 10, gameEventId: 6, ...extra })
  // LobbyEventType: DrinkAwarded 4, DrinkAssigned 5, NewPick 3.
  const items = buildFeedItems(
    [game(10, [goal])],
    [drinkEvent('awarded', 4), drinkEvent('assigned', 5), drinkEvent('other goal', 5, { gameEventId: 7 }), drinkEvent('a pick', 3)],
    lobbyCreated,
    false
  )
  const byId = (id: string) => items.find((i) => i.lobbyEvent?.id === id)!

  it('finds the goal a drink event came from, and nothing for other events', () => {
    expect(goalKeyFor(byId('awarded'))).toBe('play-10-6')
    expect(goalKeyFor(byId('assigned'))).toBe('play-10-6')
    expect(goalKeyFor(byId('a pick'))).toBeUndefined()
    expect(goalKeyFor(items.find((i) => i.key === 'play-10-6')!)).toBeUndefined()
  })

  it("lights up a goal and only that goal's drink events", () => {
    expect(chainKeys(items, 'play-10-6')).toEqual(['play-10-6', 'event-awarded', 'event-assigned'])
  })

  it("lists where a goal's drinks went, handed out or not", () => {
    const drink = (eventId: number, to?: string) => ({ eventId, recipientLobbyMemberId: to })
    const lobby = {
      members: [
        { id: 'tyler', name: 'tyler', picks: [{ gameId: 10, isActive: true, drinks: [drink(6, 'casey'), drink(9, 'casey')] }] },
        { id: 'jordan', name: 'Jordan', picks: [{ gameId: 10, isActive: true, drinks: [drink(6)] }, { gameId: 10, isActive: false, drinks: [drink(6, 'tyler')] }] },
        { id: 'casey', name: 'Casey', picks: [] }
      ]
    } as unknown as Lobby

    expect(goalDrinks(lobby, 10, 6)).toEqual([{ earner: 'tyler', recipient: 'Casey' }, { earner: 'Jordan', recipient: undefined }])
  })
})
