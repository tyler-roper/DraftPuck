import { describe, expect, it } from 'vitest'
import { parseLobbyEventText } from '@/helpers/lobbyEventTemplateHelpers'

const homeTeam = { id: 1, abbreviation: 'EDM' }
const awayTeam = { id: 2, abbreviation: 'TBL' }

function event(text: string, extra: Partial<LobbyEvent> = {}): LobbyEvent {
  return { id: 'e1', text, title: '', lobbyMemberId: 'm1', lobbyMember2Id: 'm2', gameId: 10, playerId: 97, ...extra } as unknown as LobbyEvent
}

function lobby(...members: { id: string; name: string }[]): Lobby {
  return { members } as unknown as Lobby
}

const games = [
  {
    id: 10,
    homeTeam,
    awayTeam,
    playerSummaries: [{ id: 97, firstName: 'Connor', lastName: 'McDavid' }]
  }
] as unknown as Game[]

describe('parseLobbyEventText', () => {
  it('fills in member and player names', () => {
    const result = parseLobbyEventText(
      event('{{name}} gave {{recipientName}} a drink for {{player}}'),
      lobby({ id: 'm1', name: 'Sam' }, { id: 'm2', name: 'Alex' }),
      games
    )

    expect(result.text).toBe('<strong>Sam</strong> gave <strong>Alex</strong> a drink for <strong>Connor McDavid</strong>')
  })

  it('escapes member names, since the text is rendered with v-html', () => {
    const result = parseLobbyEventText(
      event('{{name}} joined'),
      lobby({ id: 'm1', name: '<img src=x onerror=alert(1)>' }),
      games
    )

    expect(result.text).not.toContain('<img')
    expect(result.text).toContain('&lt;img src=x onerror=alert(1)&gt;')
  })

  it("shows names containing replacement patterns like $& literally", () => {
    const result = parseLobbyEventText(event('{{name}} joined'), lobby({ id: 'm1', name: "Ca$h $& $'" }), games)

    expect(result.text).toBe("<strong>Ca$h $&amp; $&#039;</strong> joined")
  })

  it('uses placeholders for members who are no longer in the lobby', () => {
    const result = parseLobbyEventText(event('{{name}} gave {{recipientName}} a drink'), lobby(), games)

    expect(result.text).toBe('<strong>(name)</strong> gave <strong>(recipient)</strong> a drink')
  })

  it('does not modify the original event', () => {
    const original = event('{{name}} joined')

    parseLobbyEventText(original, lobby({ id: 'm1', name: 'Sam' }), games)

    expect(original.text).toBe('{{name}} joined')
  })

  it('uses the light logo for Tampa Bay', () => {
    const result = parseLobbyEventText(event('{{playerBadge}}', { teamId: 2 }), lobby(), games)

    expect(result.text).toContain('/img/logos/TBL_LIGHT.png')
  })
})
