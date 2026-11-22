import { describe, expect, it } from 'vitest'
import { useFeedFocus } from '@/composables/useFeedFocus'
import LobbyEventType from '@/enums/lobbyEventType'
import type FeedItem from '@/models/feedItem'

const goal = { key: 'play-1-10', layout: 'goal', scorer: 'C. McDavid' } as FeedItem
const otherGoal = { key: 'play-1-11', layout: 'goal' } as FeedItem
const drink = {
  key: 'event-5',
  layout: 'lobby',
  lobbyEvent: { lobbyEventType: LobbyEventType.DrinkAwarded, gameId: 1, gameEventId: 10 }
} as FeedItem
const join = { key: 'event-6', layout: 'lobby' } as FeedItem

describe('useFeedFocus', () => {
  const setup = () => useFeedFocus(() => [goal, otherGoal, drink, join])

  it('collapses nothing until a link is followed', () => {
    const { focus, isCollapsed } = setup()

    expect(focus.value).toBeUndefined()
    expect(isCollapsed(join.key)).toBe(false)
  })

  it('keeps the goal and its drinks, and collapses everything else', () => {
    const { follow, isCollapsed } = setup()

    expect(follow(goal.key)).toBe(true)

    expect([goal, drink].map((i) => isCollapsed(i.key))).toEqual([false, false])
    expect([otherGoal, join].map((i) => isCollapsed(i.key))).toEqual([true, true])
  })

  it("names the scorer's goal, or just 'this goal'", () => {
    const { follow, focus } = setup()

    follow(goal.key)
    expect(focus.value?.label).toBe("C. McDavid's goal")

    follow(otherGoal.key)
    expect(focus.value?.label).toBe('this goal')
  })

  it('clears when the same link is followed again', () => {
    const { follow, focus } = setup()

    follow(goal.key)

    expect(follow(goal.key)).toBe(false)
    expect(focus.value).toBeUndefined()
  })

  it('clears on "Show all"', () => {
    const { follow, clear, isCollapsed } = setup()

    follow(goal.key)
    clear()

    expect(isCollapsed(join.key)).toBe(false)
  })
})
