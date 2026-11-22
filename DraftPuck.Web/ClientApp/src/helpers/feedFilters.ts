import FeedItemType from '@/enums/feedItemType'
import LobbyEventType from '@/enums/lobbyEventType'
import PlayType from '@/enums/playType'
import type FeedItem from '@/models/feedItem'

/** A switch in the feed's settings: which items it shows. The key is how it's saved, so it must not change. */
export interface FeedFilter {
  key: string
  label: string
  subTypes: (PlayType | LobbyEventType)[]
}

export interface FeedFilterGroup {
  title: string
  // Game events and lobby events number their types separately, so a filter only applies to items of its group's type.
  type: FeedItemType
  filters: FeedFilter[]
}

/** On (shown) or off, by filter key. */
export type FeedFilters = Record<string, boolean>

export const FEED_FILTER_GROUPS: FeedFilterGroup[] = [
  {
    title: 'Game Events',
    type: FeedItemType.GameEvent,
    filters: [
      { key: 'showGoals', label: 'Goal', subTypes: [PlayType.Goal] },
      { key: 'showPenalties', label: 'Penalty', subTypes: [PlayType.Penalty] },
      { key: 'showPeriodStarts', label: 'Period Start', subTypes: [PlayType.PeriodStart] },
      { key: 'showPeriodEnds', label: 'Period End', subTypes: [PlayType.PeriodEnd] },
      { key: 'showGameEnds', label: 'Game End', subTypes: [PlayType.GameEnd] },
      { key: 'showChallenges', label: "Coach's Challenge", subTypes: [PlayType.Challenge] }
    ]
  },
  {
    title: 'Lobby Events',
    type: FeedItemType.LobbyEvent,
    filters: [
      { key: 'showUserJoin', label: 'User Joined', subTypes: [LobbyEventType.UserJoined] },
      { key: 'showNameChange', label: 'Name Change', subTypes: [LobbyEventType.UserNameChanged] },
      { key: 'showPicks', label: 'Pick Player', subTypes: [LobbyEventType.NewPick, LobbyEventType.BotPicksMade] },
      { key: 'showDrinkAwarded', label: 'Drink Awarded', subTypes: [LobbyEventType.DrinkAwarded] },
      { key: 'showDrinkAssigned', label: 'Drink Assigned', subTypes: [LobbyEventType.DrinkAssigned] }
    ]
  }
]

/** Everything shown. */
export function defaultFeedFilters(): FeedFilters {
  return Object.fromEntries(FEED_FILTER_GROUPS.flatMap((g) => g.filters).map((f) => [f.key, true]))
}

/** Saved filters over the defaults, so a filter added since they were saved starts on. Unreadable ones are ignored. */
export function readFeedFilters(saved: string | null): FeedFilters {
  const filters = defaultFeedFilters()
  if (!saved) return filters

  try {
    const parsed: unknown = JSON.parse(saved)
    if (parsed && typeof parsed === 'object')
      for (const [key, value] of Object.entries(parsed)) if (key in filters && typeof value === 'boolean') filters[key] = value
  } catch {
    // Keep the defaults.
  }
  return filters
}

/** Whether every filter in the group is on. */
export function isGroupShown(filters: FeedFilters, group: FeedFilterGroup): boolean {
  return group.filters.every((f) => filters[f.key])
}

/** The filters with every one in the group turned on or off. */
export function setGroupShown(filters: FeedFilters, group: FeedFilterGroup, shown: boolean): FeedFilters {
  return { ...filters, ...Object.fromEntries(group.filters.map((f) => [f.key, shown])) }
}

/**
 * The items the filters let through. Items no filter covers always show. A period's end right before the game's end
 * (in the same game) is left out: the game end says it.
 */
export function filterFeedItems(items: FeedItem[], filters: FeedFilters): FeedItem[] {
  return items.filter((item, i) => {
    const group = FEED_FILTER_GROUPS.find((g) => g.type === item.type)
    const filter = group?.filters.find((f) => f.subTypes.includes(item.subType))
    if (filter && !filters[filter.key]) return false

    const next = items[i + 1]
    const isEndBeforeGameEnd = !!next && item.subType === PlayType.PeriodEnd && next.subType === PlayType.GameEnd && next.gameId === item.gameId
    return !isEndBeforeGameEnd
  })
}
