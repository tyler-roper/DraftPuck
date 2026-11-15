import { ref } from 'vue'

/** Which card in a list is flipped to its back. At most one is: flipping another turns the first back. */
export function useSingleFlip() {
  const flippedId = ref<string | null>(null)

  const isFlipped = (id: string) => flippedId.value === id

  function toggle(id: string) {
    flippedId.value = isFlipped(id) ? null : id
  }

  function reset() {
    flippedId.value = null
  }

  return { isFlipped, toggle, reset }
}
