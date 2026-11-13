import { describe, expect, it } from 'vitest'
import PeriodType from '@/enums/periodType'
import { getOrdinal } from '@/helpers/gameHelpers'
import { isIsoDateString, parseAllDates } from '@/helpers/dateHelpers'

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
    const body: any = {
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
    expect(isIsoDateString('2026-11-13' as any)).toBe(false)
    expect(isIsoDateString('Game on 2026-11-13T00:00:00' as any)).toBe(false)
  })
})
