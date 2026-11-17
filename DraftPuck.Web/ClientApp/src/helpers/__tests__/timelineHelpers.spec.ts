import { describe, expect, it } from 'vitest'
import { drinkTimeline } from '@/helpers/timelineHelpers'

const at = (minute: number) => new Date(Date.UTC(2026, 10, 16, 1, minute))
const drink = (id: string, to: string | null, assignedMinute: number | null, message?: string) => ({
  id,
  recipientLobbyMemberId: to,
  created: at(0),
  assigned: assignedMinute == null ? null : at(assignedMinute),
  message
})

const lobby = {
  members: [
    { id: 'tyler', name: 'tyler', picks: [{ gameId: 1, playerId: 97, teamId: 22, isActive: true, drinks: [drink('d1', 'casey', 5, 'Revenge.'), drink('d2', 'casey', 9), drink('d3', null, null)] }] },
    { id: 'casey', name: 'Casey', picks: [{ gameId: 1, playerId: 91, teamId: 20, isActive: true, drinks: [drink('d4', 'tyler', 7)] }] },
    { id: 'jordan', name: 'Jordan', picks: [{ gameId: 1, playerId: 29, teamId: 22, isActive: false, drinks: [drink('d5', 'tyler', 8)] }] }
  ]
} as unknown as Lobby

const games = [
  {
    id: 1,
    homeTeam: { id: 20, abbreviation: 'CGY' },
    awayTeam: { id: 22, abbreviation: 'EDM' },
    playerSummaries: [
      { id: 97, lastName: 'McDavid' },
      { id: 91, lastName: 'Kadri' }
    ]
  }
] as unknown as Game[]

describe('drinkTimeline', () => {
  const timeline = drinkTimeline(lobby, games)

  it("lists the drinks handed out (not ones still to give, nor removed picks'), newest first", () => {
    expect(timeline.map((d) => d.id)).toEqual(['d2', 'd4', 'd1'])
  })

  it('names the giver, the recipient, the player whose goal earned it, and the message', () => {
    const first = timeline.find((d) => d.id === 'd1')!
    expect([first.giver.name, first.recipient.name, first.message]).toEqual(['tyler', 'Casey', 'Revenge.'])
    expect(first.player).toMatchObject({ lastName: 'McDavid', abbreviation: 'EDM' })
  })
})
