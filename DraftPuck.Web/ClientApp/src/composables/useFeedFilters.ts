import { ref, watch } from 'vue'
import { readFeedFilters, type FeedFilters } from '@/helpers/feedFilters'

const STORAGE_KEY = 'feedFilters'

/** The feed's filters, remembered on this device. Storage that's blocked (private mode) just isn't remembered. */
export function useFeedFilters() {
  const filters = ref<FeedFilters>(readFeedFilters(load()))

  watch(filters, (value) => save(value), { deep: true })

  return { filters }
}

function load(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

function save(filters: FeedFilters) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filters))
  } catch {
    // Not remembered.
  }
}
