import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope } from 'vue'
import { useDrinkQueue } from '@/composables/useDrinkQueue'

const drink = (id: string) => ({ id }) as LobbyEvent

describe('useDrinkQueue', () => {
  let scope: ReturnType<typeof effectScope>
  const create = () => {
    scope = effectScope()
    return scope.run(() => useDrinkQueue(5000))!
  }

  beforeEach(() => vi.useFakeTimers())
  afterEach(() => {
    scope?.stop()
    vi.useRealTimers()
  })

  it('shows each drink for the duration, one at a time, in order', async () => {
    const queue = create()

    queue.enqueue(drink('a'))
    queue.enqueue(drink('b'))
    expect(queue.current.value?.id).toBe('a')

    await vi.advanceTimersByTimeAsync(4999)
    expect(queue.current.value?.id).toBe('a')

    await vi.advanceTimersByTimeAsync(1)
    expect(queue.current.value?.id).toBe('b')

    await vi.advanceTimersByTimeAsync(5000)
    expect(queue.current.value).toBeUndefined()
  })

  it('starts again for a drink that arrives after the queue emptied', async () => {
    const queue = create()
    queue.enqueue(drink('a'))
    await vi.advanceTimersByTimeAsync(5000)

    queue.enqueue(drink('b'))

    expect(queue.current.value?.id).toBe('b')
  })

  it('stops when its component goes away', async () => {
    const queue = create()
    queue.enqueue(drink('a'))
    queue.enqueue(drink('b'))

    scope.stop()
    await vi.advanceTimersByTimeAsync(20_000)

    expect(queue.current.value?.id).toBe('a')
  })
})
