import { describe, expect, it } from 'vitest'
import { memberNight, nightChart, stretchLabel } from '@/helpers/statsHelpers'

const at = (minute: number, second = 0) => new Date(2026, 10, 16, 21, minute, second)
const drink = (to: string | null, earned: number, handedOut: number | null, earnedSecond = 0, handedSecond = 0) => ({
  recipientLobbyMemberId: to,
  created: at(earned, earnedSecond),
  assigned: handedOut == null ? null : at(handedOut, handedSecond)
})

// tyler: Makar earned 3 (two to Casey, one still to give), Hyman earned 1 (to Riley).
// casey: Kadri earned 1 (to tyler). riley: a removed pick's drink doesn't count.
const lobby = {
  members: [
    {
      id: 'tyler',
      name: 'tyler',
      picks: [
        { gameId: 1, playerId: 8, isActive: true, drinks: [drink('casey', 0, 0, 0, 8), drink('casey', 5, 12), drink(null, 30, null)] },
        { gameId: 1, playerId: 18, isActive: true, drinks: [drink('riley', 14, 15)] },
        { gameId: 1, playerId: 99, isActive: true, drinks: [] }
      ]
    },
    { id: 'casey', name: 'Casey', picks: [{ gameId: 1, playerId: 91, isActive: true, drinks: [drink('tyler', 13, 14)] }] },
    { id: 'riley', name: 'Riley', picks: [{ gameId: 1, playerId: 29, isActive: false, drinks: [drink('tyler', 1, 1)] }] }
  ]
} as unknown as Lobby

const players: RecapPlayer[] = [
  { gameId: 1, playerId: 8, name: 'Cale Makar', team: 'COL' },
  { gameId: 1, playerId: 18, name: 'Zach Hyman', team: 'EDM' },
  { gameId: 1, playerId: 91, name: 'Nazem Kadri', team: 'CGY' }
]

describe('memberNight', () => {
  const night = memberNight(lobby, players, 'tyler')

  it("lists what each of the member's picks earned, best first", () => {
    expect(night.picks).toEqual([
      { name: 'Cale Makar', team: 'COL', drinks: 3 },
      { name: 'Zach Hyman', team: 'EDM', drinks: 1 },
      { name: 'Unknown player', team: '', drinks: 0 }
    ])
  })

  it('counts who they made drink, and who made them drink (not from removed picks)', () => {
    expect(night.gaveTo.map((c) => [c.member.name, c.count])).toEqual([
      ['Casey', 2],
      ['Riley', 1]
    ])
    expect(night.gotFrom.map((c) => [c.member.name, c.count])).toEqual([['Casey', 1]])
  })
})

describe('nightChart', () => {
  it('buckets the drinks handed out by 10 minutes, with those involving the member', () => {
    const bars = nightChart(lobby, players, 'riley')
    expect(bars.map((b) => [b.start.getMinutes(), b.total, b.involving])).toEqual([
      [0, 1, 0],
      [10, 3, 1]
    ])
  })

  it("lists each stretch's drinks: who gave, who took, and the player", () => {
    const [first] = nightChart(lobby, players, 'tyler')
    expect(first.drinks.map((d) => [d.giver.name, d.recipient.name, d.player])).toEqual([['tyler', 'Casey', 'Cale Makar']])
  })

  it('is empty when nobody drank', () => {
    expect(nightChart({ members: [] } as unknown as Lobby, players, 'tyler')).toEqual([])
  })
})

describe('stretchLabel', () => {
  it("is the stretch's start and end times", () => {
    const start = new Date(2026, 10, 22, 21, 10)
    const end = new Date(2026, 10, 22, 21, 20)

    expect(stretchLabel({ start, end })).toBe('9:10–9:20')
  })
})
