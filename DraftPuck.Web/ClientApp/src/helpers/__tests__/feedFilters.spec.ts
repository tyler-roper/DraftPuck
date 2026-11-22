import { describe, expect, it } from 'vitest'
import { defaultFeedFilters, FEED_FILTER_GROUPS, filterFeedItems, isGroupShown, readFeedFilters, setGroupShown } from '@/helpers/feedFilters'
import FeedItemType from '@/enums/feedItemType'
import LobbyEventType from '@/enums/lobbyEventType'
import PlayType from '@/enums/playType'
import type FeedItem from '@/models/feedItem'

const [gameEvents, lobbyEvents] = FEED_FILTER_GROUPS

function gameItem(key: string, subType: PlayType, gameId = 1): FeedItem {
  return { key, type: FeedItemType.GameEvent, subType, gameId } as FeedItem
}

function lobbyItem(key: string, subType: LobbyEventType): FeedItem {
  return { key, type: FeedItemType.LobbyEvent, subType, gameId: undefined } as FeedItem
}

const keys = (items: FeedItem[]) => items.map((i) => i.key)

describe('filterFeedItems', () => {
  it('shows everything with the default filters', () => {
    const items = [gameItem('goal', PlayType.Goal), lobbyItem('pick', LobbyEventType.NewPick)]

    expect(keys(filterFeedItems(items, defaultFeedFilters()))).toEqual(['goal', 'pick'])
  })

  it('hides the types a filter turned off covers', () => {
    const items = [gameItem('goal', PlayType.Goal), gameItem('penalty', PlayType.Penalty)]

    expect(keys(filterFeedItems(items, { ...defaultFeedFilters(), showPenalties: false }))).toEqual(['goal'])
  })

  it("hides both members' and bots' picks with Pick Player", () => {
    const items = [lobbyItem('pick', LobbyEventType.NewPick), lobbyItem('bot', LobbyEventType.BotPicksMade)]

    expect(filterFeedItems(items, { ...defaultFeedFilters(), showPicks: false })).toEqual([])
  })

  it("applies a filter only to its own group's items, though game and lobby types share numbers", () => {
    const sameNumberAsGoal = PlayType.Goal as unknown as LobbyEventType
    const items = [lobbyItem('lobby', sameNumberAsGoal)]

    expect(keys(filterFeedItems(items, { ...defaultFeedFilters(), showGoals: false }))).toEqual(['lobby'])
  })

  it('always shows items no filter covers', () => {
    const items = [lobbyItem('broadcast', LobbyEventType.Broadcast)]

    expect(keys(filterFeedItems(items, setGroupShown(defaultFeedFilters(), lobbyEvents, false)))).toEqual(['broadcast'])
  })

  it("leaves out a period's end right before its game's end", () => {
    const items = [gameItem('end', PlayType.PeriodEnd), gameItem('final', PlayType.GameEnd), gameItem('other', PlayType.PeriodEnd, 2)]

    expect(keys(filterFeedItems(items, defaultFeedFilters()))).toEqual(['final', 'other'])
  })

  it("keeps a period's end before another game's end", () => {
    const items = [gameItem('end', PlayType.PeriodEnd, 1), gameItem('final', PlayType.GameEnd, 2)]

    expect(keys(filterFeedItems(items, defaultFeedFilters()))).toEqual(['end', 'final'])
  })
})

describe('readFeedFilters', () => {
  it('is the defaults when nothing was saved, or what was saved is unreadable', () => {
    expect(readFeedFilters(null)).toEqual(defaultFeedFilters())
    expect(readFeedFilters('{nope')).toEqual(defaultFeedFilters())
  })

  it('keeps saved choices, and starts filters added since then on', () => {
    const filters = readFeedFilters(JSON.stringify({ showGoals: false }))

    expect(filters.showGoals).toBe(false)
    expect(filters.showDrinkAssigned).toBe(true)
  })

  it('ignores keys and values it does not know', () => {
    const filters = readFeedFilters(JSON.stringify({ showGoals: 'no', retired: false }))

    expect(filters).toEqual(defaultFeedFilters())
  })
})

describe('feed filter groups', () => {
  it('turns a whole group on or off, leaving the other group alone', () => {
    const hidden = setGroupShown(defaultFeedFilters(), gameEvents, false)

    expect(isGroupShown(hidden, gameEvents)).toBe(false)
    expect(gameEvents.filters.some((f) => hidden[f.key])).toBe(false)
    expect(isGroupShown(hidden, lobbyEvents)).toBe(true)
    expect(isGroupShown(setGroupShown(hidden, gameEvents, true), gameEvents)).toBe(true)
  })

  it('counts a group as shown only when every filter in it is on', () => {
    expect(isGroupShown({ ...defaultFeedFilters(), showChallenges: false }, gameEvents)).toBe(false)
  })
})
