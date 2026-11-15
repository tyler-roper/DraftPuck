import { describe, expect, it } from 'vitest'
import { backfillProgress, describeBackfill, type AchievementBackfill } from '@/helpers/backfillHelpers'

const run = (overrides: Partial<AchievementBackfill>): AchievementBackfill => ({
  id: 'run',
  startedUtc: '2026-11-15T16:00:00Z',
  usersQueued: 450,
  usersChecked: 0,
  achievementsAwarded: 0,
  isComplete: false,
  ...overrides
})

describe('describeBackfill', () => {
  it('shows progress while running', () => {
    expect(describeBackfill(run({ usersChecked: 120, achievementsAwarded: 87 }))).toBe('Checking… 120 of 450 players, 87 achievements awarded so far.')
  })

  it('shows the result when done', () => {
    expect(describeBackfill(run({ usersChecked: 450, achievementsAwarded: 1312, isComplete: true }))).toBe(
      'Done: 450 players checked, 1,312 achievements awarded.'
    )
  })

  it('uses the singular for one', () => {
    expect(describeBackfill(run({ usersQueued: 1, usersChecked: 1, achievementsAwarded: 1, isComplete: true }))).toBe(
      'Done: 1 player checked, 1 achievement awarded.'
    )
  })
})

describe('backfillProgress', () => {
  it('is the share of players checked', () => {
    expect(backfillProgress(run({ usersChecked: 225 }))).toBe(50)
  })

  it('is complete when there was nobody to check', () => {
    expect(backfillProgress(run({ usersQueued: 0, isComplete: true }))).toBe(100)
  })
})
