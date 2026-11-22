import { describe, expect, it } from 'vitest'
import { friendlyPosition, goalsInPeriod, isCriticalMoment, linescoreColumns, pickerNames, powerPlayLabel, scorerSummary } from '@/helpers/scoreboard'
import GameState from '@/enums/gameState'
import PeriodType from '@/enums/periodType'
import PlayType from '@/enums/playType'

const HOME = 1
const AWAY = 2

function team(id: number, extra: Partial<GameTeam> = {}): GameTeam {
  return { id, score: 0, strength: 5, roster: [], ...extra } as unknown as GameTeam
}

function game(extra: Partial<Game> = {}): Game {
  return {
    id: 10,
    gameState: GameState.Live,
    period: 2,
    periodType: PeriodType.Regulation,
    minutesRemainingInPeriod: 12,
    goalsByPeriod: [],
    plays: [],
    homeTeam: team(HOME),
    awayTeam: team(AWAY),
    ...extra
  } as unknown as Game
}

const period = (number: number, homeGoals = 0, awayGoals = 0, periodType = PeriodType.Regulation) => ({ number, periodType, homeGoals, awayGoals })

describe('linescoreColumns', () => {
  it('always has periods 1 to 3, with only the ones reached showing goals', () => {
    const columns = linescoreColumns(game({ period: 2, goalsByPeriod: [period(1), period(2)] }))

    expect(columns).toEqual([
      { number: 1, label: '1', isReached: true },
      { number: 2, label: '2', isReached: true },
      { number: 3, label: '3', isReached: false }
    ])
  })

  it('adds overtime and the shootout, by name', () => {
    const g = game({
      period: 5,
      goalsByPeriod: [period(1), period(2), period(3), period(4, 0, 0, PeriodType.Overtime), period(5, 0, 0, PeriodType.Shootout)]
    })

    expect(linescoreColumns(g).map((c) => c.label)).toEqual(['1', '2', '3', 'OT', 'SO'])
  })

  it('has empty periods 1 to 3 before the game starts', () => {
    expect(linescoreColumns(game({ gameState: GameState.Upcoming, period: 0 })).every((c) => !c.isReached)).toBe(true)
  })
})

describe('goalsInPeriod', () => {
  it("is the team's goals in that period, or 0 if it hasn't been played", () => {
    const g = game({ goalsByPeriod: [period(1, 2, 1)] })

    expect(goalsInPeriod(g, g.homeTeam, 1)).toBe(2)
    expect(goalsInPeriod(g, g.awayTeam, 1)).toBe(1)
    expect(goalsInPeriod(g, g.homeTeam, 2)).toBe(0)
  })
})

describe('scorerSummary', () => {
  const goal = (playerId: number, teamId = HOME) => ({ type: PlayType.Goal, primaryPlayerId: playerId, primaryTeamId: teamId }) as Play
  const roster = [
    { id: 9, firstName: 'Sebastian', lastName: 'Aho' },
    { id: 3, firstName: 'Jackson', lastName: 'Blake' }
  ] as Player[]

  it("lists the team's scorers in the order they first scored, counting more than one", () => {
    const g = game({ homeTeam: team(HOME, { roster }), plays: [goal(9), goal(3), goal(50, AWAY), goal(9)] })

    expect(scorerSummary(g, g.homeTeam)).toBe('S Aho (2), J Blake')
  })

  it("ignores other plays and scorers who aren't on the roster", () => {
    const g = game({ homeTeam: team(HOME, { roster }), plays: [{ ...goal(9), type: PlayType.Penalty }, goal(77)] })

    expect(scorerSummary(g, g.homeTeam)).toBe('')
  })
})

describe('powerPlayLabel', () => {
  it('is "PP" for 5-on-4 either way, and the strengths otherwise', () => {
    expect(powerPlayLabel(game({ homeTeam: team(HOME, { strength: 4 }), awayTeam: team(AWAY, { strength: 5 }) }))).toBe('PP')
    expect(powerPlayLabel(game({ homeTeam: team(HOME, { strength: 5 }), awayTeam: team(AWAY, { strength: 3 }) }))).toBe('5-on-3')
  })
})

describe('isCriticalMoment', () => {
  const late3rd = { period: 3, minutesRemainingInPeriod: 4 }

  it('is overtime', () => {
    expect(isCriticalMoment(game({ period: 4 }))).toBe(true)
  })

  it('is the last five minutes of a third period within a goal', () => {
    expect(isCriticalMoment(game({ ...late3rd, homeTeam: team(HOME, { score: 2 }), awayTeam: team(AWAY, { score: 1 }) }))).toBe(true)
    expect(isCriticalMoment(game({ ...late3rd, homeTeam: team(HOME, { score: 3 }), awayTeam: team(AWAY, { score: 1 }) }))).toBe(false)
    expect(isCriticalMoment(game({ period: 3, minutesRemainingInPeriod: 5 }))).toBe(false)
  })

  it('is never a game that is not live', () => {
    expect(isCriticalMoment(game({ gameState: GameState.Final, period: 4 }))).toBe(false)
  })
})

describe('friendlyPosition', () => {
  it('names wingers LW and RW', () => {
    expect(['L', 'R', 'C', 'D'].map(friendlyPosition)).toEqual(['LW', 'RW', 'C', 'D'])
  })
})

describe('pickerNames', () => {
  it("names who picked each player in the game, ignoring other games' picks", () => {
    const lobby = {
      members: [
        { name: 'Sam', picks: [{ gameId: 10, playerId: 9 }] },
        {
          name: 'Max',
          picks: [
            { gameId: 10, playerId: 3 },
            { gameId: 11, playerId: 7 }
          ]
        }
      ]
    } as unknown as Lobby

    expect([...pickerNames(lobby, 10)]).toEqual([
      [9, 'Sam'],
      [3, 'Max']
    ])
  })
})
