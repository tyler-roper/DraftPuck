import { describe, expect, it } from 'vitest'
import { goalScoreline } from '@/helpers/scorelineHelpers'

const HOME = 1
const AWAY = 2
const goal = (primaryTeamId: number | null, homeScore: number, awayScore: number) => ({ primaryTeamId, homeScore, awayScore })

describe('goalScoreline', () => {
  it("puts the leader's score first and highlights the scorer's: the leader scoring", () => {
    expect(goalScoreline(goal(HOME, 5, 4), HOME)).toEqual({ first: 5, second: 4, highlighted: 'first' })
  })

  it('the trailing team scoring', () => {
    expect(goalScoreline(goal(AWAY, 5, 4), HOME)).toEqual({ first: 5, second: 4, highlighted: 'second' })
  })

  it('a tying goal is highlighted second: the scorer caught up', () => {
    expect(goalScoreline(goal(HOME, 4, 4), HOME)).toEqual({ first: 4, second: 4, highlighted: 'second' })
  })

  it('highlights nothing when the scoring team is unknown', () => {
    expect(goalScoreline(goal(null, 2, 3), HOME)).toEqual({ first: 3, second: 2, highlighted: null })
  })
})
