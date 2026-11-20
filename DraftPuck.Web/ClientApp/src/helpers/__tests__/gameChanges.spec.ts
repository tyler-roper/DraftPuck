import { describe, expect, it } from 'vitest'
import { applyGameChanges } from '@/helpers/gameChanges'
import GameState from '@/enums/gameState'

const at = (second: number) => new Date(Date.UTC(2026, 10, 19, 0, 0, second))
const play = (id: number, homeScore = 0) => ({ id, homeScore }) as unknown as Play
const player = (id: number) => ({ id }) as unknown as Player

function game(): Game {
  return {
    id: 1,
    updatedUtc: at(0),
    gameState: GameState.Live,
    timeRemainingInPeriod: '12:00',
    plays: [play(1), play(2)],
    homeTeam: { name: 'Home', score: 1, roster: [player(100)], situations: [], strength: 5 },
    awayTeam: { name: 'Away', score: 0, roster: [player(200)], situations: [], strength: 5 },
    playerSummaries: [{ id: 100 }, { id: 200 }],
    formerPlayers: []
  } as unknown as Game
}

function changes(overrides: Partial<GameChanges> = {}): GameChanges {
  return {
    id: 1,
    baseUpdatedUtc: at(0),
    updatedUtc: at(10),
    gameState: GameState.Live,
    timeRemainingInPeriod: '11:50',
    playIds: [1, 2],
    plays: [],
    homeTeam: { score: 1, situations: [], strength: 5, roster: null },
    awayTeam: { score: 0, situations: [], strength: 5, roster: null },
    playerSummaries: null,
    formerPlayers: null,
    ...overrides
  } as unknown as GameChanges
}

describe('applyGameChanges', () => {
  it('updates the clock and score, and keeps what was not sent', () => {
    const held = game()
    const roster = held.homeTeam.roster
    const plays = [...held.plays]

    const applied = applyGameChanges(held, changes({ homeTeam: { score: 2, situations: [], strength: 5, roster: null } }))

    expect(applied).toBe(true)
    expect(held.updatedUtc).toEqual(at(10))
    expect(held.timeRemainingInPeriod).toBe('11:50')
    expect(held.homeTeam.score).toBe(2)
    expect(held.homeTeam.name).toBe('Home')
    expect(held.homeTeam.roster).toBe(roster)
    expect(held.plays).toEqual(plays)
    expect(held.playerSummaries).toHaveLength(2)
  })

  it('adds new plays, replaces changed ones, and drops removed ones, in the order sent', () => {
    const held = game()
    const unchanged = held.plays[0]

    applyGameChanges(held, changes({ playIds: [1, 3, 2], plays: [play(3), play(2, 1)] }))
    expect(held.plays.map((p) => p.id)).toEqual([1, 3, 2])
    expect(held.plays[0]).toBe(unchanged)
    expect(held.plays[2].homeScore).toBe(1)

    applyGameChanges(held, changes({ baseUpdatedUtc: at(10), updatedUtc: at(20), playIds: [1, 3] }))
    expect(held.plays.map((p) => p.id)).toEqual([1, 3])
  })

  it('replaces rosters and lineups that were sent', () => {
    const held = game()

    applyGameChanges(
      held,
      changes({
        awayTeam: { score: 0, situations: [], strength: 5, roster: [player(200), player(201)] },
        playerSummaries: [{ id: 100 }] as PlayerSummary[],
        formerPlayers: [{ id: 200 }] as PlayerSummary[]
      })
    )

    expect(held.awayTeam.roster.map((p) => p.id)).toEqual([200, 201])
    expect(held.playerSummaries.map((p) => p.id)).toEqual([100])
    expect(held.formerPlayers!.map((p) => p.id)).toEqual([200])
  })

  it('takes a whole game (no base) whatever copy is held', () => {
    const held = game()

    expect(applyGameChanges(held, changes({ baseUpdatedUtc: null, updatedUtc: at(30), playIds: [5], plays: [play(5)] }))).toBe(true)
    expect(held.plays.map((p) => p.id)).toEqual([5])
  })

  it("won't apply to a copy other than the one the changes were made from", () => {
    const held = game()

    expect(applyGameChanges(held, changes({ baseUpdatedUtc: at(5), timeRemainingInPeriod: '1:00' }))).toBe(false)
    expect(held.timeRemainingInPeriod).toBe('12:00')
  })

  it("won't apply when it keeps a play that isn't held", () => {
    const held = game()

    expect(applyGameChanges(held, changes({ playIds: [1, 2, 9] }))).toBe(false)
    expect(held.updatedUtc).toEqual(at(0))
  })
})
