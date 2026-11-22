import { describe, expect, it } from 'vitest'
import { timeAgo } from '@/helpers/dateHelpers'

const now = new Date(2026, 10, 22, 21, 0)
const daysBefore = (days: number, hour = 19) => new Date(2026, 10, 22 - days, hour, 0)

describe('timeAgo', () => {
  it('says Today for earlier today, or the label given', () => {
    expect(timeAgo(daysBefore(0), now)).toBe('Today')
    expect(timeAgo(daysBefore(0), now, 'Tonight')).toBe('Tonight')
  })

  it('says Yesterday for yesterday, even under 24 hours ago', () => {
    expect(timeAgo(daysBefore(1, 23), now)).toBe('Yesterday')
    expect(timeAgo(daysBefore(1, 8), now)).toBe('Yesterday')
  })

  it('counts calendar days, then weeks', () => {
    expect(timeAgo(daysBefore(2, 23), now)).toBe('2d ago')
    expect(timeAgo(daysBefore(6), now)).toBe('6d ago')
    expect(timeAgo(daysBefore(7), now)).toBe('1w ago')
    expect(timeAgo(daysBefore(29), now)).toBe('4w ago')
  })

  it('writes months as "mo", so they read as months, not minutes', () => {
    expect(timeAgo(daysBefore(30), now)).toBe('1mo ago')
    expect(timeAgo(daysBefore(200), now)).toBe('6mo ago')
    expect(timeAgo(daysBefore(364), now)).toBe('11mo ago')
  })

  it('counts years', () => {
    expect(timeAgo(daysBefore(365), now)).toBe('1y ago')
    expect(timeAgo(daysBefore(800), now)).toBe('2y ago')
  })
})
