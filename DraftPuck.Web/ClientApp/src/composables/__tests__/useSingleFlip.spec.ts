import { describe, expect, it } from 'vitest'
import { useSingleFlip } from '@/composables/useSingleFlip'

describe('useSingleFlip', () => {
  it('flips one card at a time: flipping another turns the first back', () => {
    const flip = useSingleFlip()

    flip.toggle('a')
    expect(flip.isFlipped('a')).toBe(true)

    flip.toggle('b')
    expect(flip.isFlipped('a')).toBe(false)
    expect(flip.isFlipped('b')).toBe(true)
  })

  it('turns a flipped card back when it is toggled again, or on reset', () => {
    const flip = useSingleFlip()

    flip.toggle('a')
    flip.toggle('a')
    expect(flip.isFlipped('a')).toBe(false)

    flip.toggle('b')
    flip.reset()
    expect(flip.isFlipped('b')).toBe(false)
  })
})
