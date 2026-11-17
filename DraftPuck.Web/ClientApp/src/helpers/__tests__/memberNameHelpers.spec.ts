import { describe, expect, it } from 'vitest'
import { memberNameScale } from '@/helpers/memberNameHelpers'

describe('memberNameScale', () => {
  it('keeps ordinary names full size', () => {
    expect(memberNameScale('tyler')).toBe(1)
    expect(memberNameScale('Fourteen Chars')).toBe(1)
  })

  it('shrinks longer names a step, and very long ones further', () => {
    expect(memberNameScale('Hingle McCringleberry')).toBe(0.85)
    expect(memberNameScale('X'.repeat(50))).toBe(0.72)
  })

  it('counts characters, not UTF-16 units, and ignores surrounding spaces', () => {
    expect(memberNameScale('🍺'.repeat(14))).toBe(1)
    expect(memberNameScale('  tyler  ')).toBe(1)
  })
})
