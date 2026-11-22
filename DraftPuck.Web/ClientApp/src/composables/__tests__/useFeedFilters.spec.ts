import { beforeEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { useFeedFilters } from '@/composables/useFeedFilters'

describe('useFeedFilters', () => {
  beforeEach(() => localStorage.clear())

  it('shows everything at first', () => {
    const { filters } = useFeedFilters()

    expect(Object.values(filters.value).every((on) => on)).toBe(true)
  })

  it('starts from the filters saved on this device', () => {
    localStorage.setItem('feedFilters', JSON.stringify({ showPenalties: false }))

    const { filters } = useFeedFilters()

    expect(filters.value.showPenalties).toBe(false)
    expect(filters.value.showGoals).toBe(true)
  })

  it('saves a change', async () => {
    const { filters } = useFeedFilters()

    filters.value.showGoals = false
    await nextTick()

    expect(JSON.parse(localStorage.getItem('feedFilters')!)).toMatchObject({ showGoals: false, showPenalties: true })
  })
})
