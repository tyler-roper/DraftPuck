import { ref, watch } from 'vue'

/** More keys than this appearing at once is a load (the page's data, a refetch), not live arrivals to animate. */
const MAX_ARRIVALS = 3

/**
 * Which items in a list just arrived, so only those animate in. A key is new when a change adds it for the first
 * time, and only a few keys at once count. So nothing animates on first render, on a bulk load, or when items that
 * were already seen come back (e.g. a filter turned back on).
 * @param keys The list's item keys, oldest first.
 */
export function useNewArrivals(keys: () => string[]) {
  const seen = new Set<string>()
  // New keys from the latest change, newest first.
  const arrivals = ref<string[]>([])
  let isFirst = true

  watch(
    keys,
    (current) => {
      const added = current.filter((key) => !seen.has(key))
      added.forEach((key) => seen.add(key))

      // A change that adds nothing (e.g. a game update rebuilding the list) leaves arrivals mid-animation alone.
      if (isFirst || added.length > 0) arrivals.value = !isFirst && added.length <= MAX_ARRIVALS ? added.reverse() : []
      isFirst = false
    },
    { immediate: true }
  )

  const isNew = (key: string) => arrivals.value.includes(key)

  /** 0 for the newest arrival, 1 for the next, ... (for staggering); -1 if not new. */
  const arrivalOrder = (key: string) => arrivals.value.indexOf(key)

  return { isNew, arrivalOrder }
}
