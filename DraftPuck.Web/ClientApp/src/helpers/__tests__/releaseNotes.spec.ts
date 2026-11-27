import { describe, expect, it } from 'vitest'
import { releaseMonthName, releases } from '@/helpers/releaseNotes'

describe('releases', () => {
  it('are newest first, one per month', () => {
    const months = releases.map((r) => r.month)
    expect(months).toEqual([...months].sort().reverse())
    expect(new Set(months).size).toBe(months.length)
  })
})

describe('releaseMonthName', () => {
  it('names the month and year', () => {
    expect(releaseMonthName('2026-11')).toBe('November 2026')
    expect(releaseMonthName('2027-01')).toBe('January 2027')
  })
})
