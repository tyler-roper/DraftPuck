import { describe, expect, it } from 'vitest'
import { buildFeedItems } from '@/helpers/feedHelpers'
import FeedItemType from '@/enums/feedItemType'
import PlayType from '@/enums/playType'
import PeriodType from '@/enums/periodType'
import LobbyEventType from '@/enums/lobbyEventType'

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

  // Like the game's own goal items: the logo of the team whose goal it was (the new scorer's, for a changed goal).
  it("shows the team's logo on goal corrections, and no logo on other lobby events", () => {
    const events = [
      lobbyEvent('removed', at(1), { lobbyEventType: LobbyEventType.GoalRemoved, gameId: 10, teamId: 2 }),
      lobbyEvent('changed', at(2), { lobbyEventType: LobbyEventType.GoalChanged, gameId: 10, teamId: 1 }),
      lobbyEvent('joined', at(3), { lobbyEventType: LobbyEventType.UserJoined, gameId: 10, teamId: 1 }),
      lobbyEvent('unknown game', at(4), { lobbyEventType: LobbyEventType.GoalRemoved, gameId: 99, teamId: 1 })
    ]

    const items = buildFeedItems([game(10, [])], events, lobbyCreated, false)

    expect(items.map((i) => i.images)).toEqual([['CGY.png'], ['EDM.png'], [], []])
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
