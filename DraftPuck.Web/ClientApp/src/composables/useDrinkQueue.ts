import { nextTick, onScopeDispose, ref } from 'vue'

/**
 * Drinks given to you, shown one at a time, each for a few seconds. Drinks that arrive while one is showing wait their
 * turn. Between drinks nothing is shown for a tick, so the animation starts again rather than swapping in place.
 */
export function useDrinkQueue(durationMs = 5000) {
  const current = ref<LobbyEvent>()
  const waiting: LobbyEvent[] = []
  let timer: number | undefined

  function enqueue(drink: LobbyEvent) {
    waiting.push(drink)
    if (timer === undefined) showNext()
  }

  function showNext() {
    const next = waiting.shift()
    if (!next) {
      timer = undefined
      return
    }

    current.value = next
    timer = window.setTimeout(async () => {
      current.value = undefined
      await nextTick()
      showNext()
    }, durationMs)
  }

  onScopeDispose(() => window.clearTimeout(timer))

  return { current, enqueue }
}
