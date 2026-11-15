import { describe, expect, it } from 'vitest'
import { nextTick, ref } from 'vue'
import { useNewArrivals } from '@/composables/useNewArrivals'

describe('useNewArrivals', () => {
  function setup(initial: string[]) {
    const keys = ref(initial)
    const arrivals = useNewArrivals(() => keys.value)
    return { keys, arrivals }
  }

  it('treats what is there at first as already seen', () => {
    const { arrivals } = setup(['a', 'b'])

    expect(arrivals.isNew('a')).toBe(false)
    expect(arrivals.isNew('b')).toBe(false)
  })

  it('marks a few items arriving together as new, in the order to animate them (newest first)', async () => {
    const { keys, arrivals } = setup(['a'])

    keys.value = ['a', 'b', 'c']
    await nextTick()

    expect(arrivals.isNew('a')).toBe(false)
    expect(arrivals.isNew('b')).toBe(true)
    expect(arrivals.arrivalOrder('c')).toBe(0)
    expect(arrivals.arrivalOrder('b')).toBe(1)
  })

  it('does not animate a bulk load, such as the first data arriving after an empty feed', async () => {
    const { keys, arrivals } = setup([])

    keys.value = ['a', 'b', 'c', 'd']
    await nextTick()

    expect(['a', 'b', 'c', 'd'].some((k) => arrivals.isNew(k))).toBe(false)
  })

  it('does not treat items that come back (e.g. a filter turned back on) as new', async () => {
    const { keys, arrivals } = setup(['a', 'b'])

    keys.value = ['a']
    await nextTick()
    keys.value = ['a', 'b']
    await nextTick()

    expect(arrivals.isNew('b')).toBe(false)
  })

  // Game updates rebuild the feed every few seconds without adding anything; an arrival mid-animation must stay new.
  it('keeps arrivals new through changes that add nothing', async () => {
    const { keys, arrivals } = setup(['a'])

    keys.value = ['a', 'b']
    await nextTick()
    keys.value = ['a', 'b']
    await nextTick()

    expect(arrivals.isNew('b')).toBe(true)
  })

  it('forgets earlier arrivals once something else arrives', async () => {
    const { keys, arrivals } = setup(['a'])

    keys.value = ['a', 'b']
    await nextTick()
    keys.value = ['a', 'b', 'c']
    await nextTick()

    expect(arrivals.isNew('b')).toBe(false)
    expect(arrivals.isNew('c')).toBe(true)
  })
})
