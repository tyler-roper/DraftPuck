import { describe, expect, it } from 'vitest'
import PeriodType from '@/enums/periodType'
import { getOrdinal } from '@/helpers/gameHelpers'
import { formatCountdown, isIsoDateString, parseAllDates } from '@/helpers/dateHelpers'

describe('getOrdinal', () => {
  it.each([
    [1, PeriodType.Regulation, '1st'],
    [2, PeriodType.Regulation, '2nd'],
    [3, PeriodType.Regulation, '3rd'],
    [4, PeriodType.Overtime, 'OT'],
    [5, PeriodType.Overtime, '2OT'],
    [7, PeriodType.Overtime, '4OT'],
    [5, PeriodType.Shootout, 'SO']
  ])('period %i (%s) is %s', (period, type, expected) => {
    expect(getOrdinal(period, type)).toBe(expected)
  })
})

describe('parseAllDates', () => {
  it('turns ISO strings into Dates throughout the response, treating them as UTC', () => {
    const body = {
      created: '2026-11-13T00:00:00',
      lobby: { members: [{ joined: '2026-11-13T01:30:00.123Z', name: 'Sam' }] },
      count: 3
    }

    parseAllDates(body)

    expect(body.created).toEqual(new Date(Date.UTC(2026, 10, 13, 0, 0, 0)))
    expect(body.lobby.members[0].joined).toEqual(new Date(Date.UTC(2026, 10, 13, 1, 30, 0, 123)))
    expect(body.lobby.members[0].name).toBe('Sam')
    expect(body.count).toBe(3)
  })

  it('ignores strings that only look partly like dates', () => {
    expect(isIsoDateString('2026-11-13')).toBe(false)
    expect(isIsoDateString('Game on 2026-11-13T00:00:00')).toBe(false)
    expect(isIsoDateString(42)).toBe(false)
  })
})

describe('formatCountdown', () => {
  const now = new Date('2026-11-15T19:00:00Z')
  const inSeconds = (seconds: number) => new Date(now.getTime() + seconds * 1000)

  // The labels date-fns 2 produced, which the pick countdown showed.
  it.each([
    [45, '45 seconds'],
    [0, '0 seconds'],
    [125, '2 minutes'],
    [3600 + 300, '1 hour 5 minutes'],
    [-30, '30 seconds']
  ])('%i seconds away is "%s"', (seconds, expected) => {
    expect(formatCountdown(inSeconds(seconds), now)).toBe(expected)
  })

  it('reads the same whichever moment comes first', () => {
    expect(formatCountdown(now, inSeconds(3900))).toBe(formatCountdown(inSeconds(3900), now))
  })

  it('includes days rather than dropping them', () => {
    expect(formatCountdown(inSeconds(26 * 3600 + 60), now)).toBe('1 day 2 hours 1 minute')
  })
})
