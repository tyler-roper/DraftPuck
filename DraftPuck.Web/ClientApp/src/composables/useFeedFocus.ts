import { computed, ref } from 'vue'
import { chainKeys } from '@/helpers/feedHelpers'
import type FeedItem from '@/models/feedItem'

export interface FeedFocus {
  goalKey: string
  keys: Set<string>
  label: string
}

/**
 * Following a drink's link to its goal focuses the feed on that goal: the goal and its drinks stay, everything else
 * collapses, until it's cleared (or the same link is followed again).
 */
export function useFeedFocus(items: () => FeedItem[]) {
  const goalsByKey = computed(
    () =>
      new Map(
        items()
          .filter((i) => i.layout === 'goal')
          .map((i) => [i.key, i])
      )
  )
  const focus = ref<FeedFocus>()

  /** Focuses on the goal; true if it did, false if that goal was already focused and it cleared instead. */
  function follow(goalKey: string): boolean {
    if (focus.value?.goalKey === goalKey) {
      clear()
      return false
    }

    const goal = goalsByKey.value.get(goalKey)
    focus.value = { goalKey, keys: new Set(chainKeys(items(), goalKey)), label: goal?.scorer ? `${goal.scorer}'s goal` : 'this goal' }
    return true
  }

  function clear() {
    focus.value = undefined
  }

  const isCollapsed = (key: string) => !!focus.value && !focus.value.keys.has(key)

  return { focus, goalsByKey, follow, clear, isCollapsed }
}
