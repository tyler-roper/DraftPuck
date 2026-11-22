import { describe, expect, it } from 'vitest'
import {
  firstTeamToPick,
  memberPicksWithPlayers,
  membersViewerFirst,
  nextPickStep,
  nextPicksCountdown,
  skatersForPicking
} from '@/helpers/picksPanel'
import { formatCountdown } from '@/helpers/dateHelpers'
import GameState from '@/enums/gameState'

const now = new Date('2026-11-22T20:00:00Z')
const opened = new Date('2026-11-22T19:30:00Z')

function player(id: number, position = 'C'): Player {
  return { id, position } as Player
}

function game(id: number, extra: Partial<Game> = {}): Game {
  return {
    id,
    gameState: GameState.Upcoming,
    pickingOpensUtc: opened,
    isPickingClosed: false,
    homeTeam: { id: id * 10 + 1, roster: [player(id * 100 + 1)] },
    awayTeam: { id: id * 10 + 2, roster: [player(id * 100 + 2)] },
    ...extra
  } as unknown as Game
}

function pick(g: Game, team: GameTeam, playerId = team.roster[0].id): LobbyMemberPick {
  return { id: `p${playerId}`, lobbyMemberId: 'me', gameId: g.id, teamId: team.id, playerId } as LobbyMemberPick
}

function member(picks: LobbyMemberPick[] = [], id = 'me'): LobbyMember {
  return { id, picks } as unknown as LobbyMember
}

function lobby(gameIds: number[], members: LobbyMember[] = []): Lobby {
  return { gameIds, picksPerTeam: 1, members } as unknown as Lobby
}

describe('memberPicksWithPlayers', () => {
  it('lists live games first, then upcoming, then final', () => {
    const final = game(1, { gameState: GameState.Final })
    const upcoming = game(2)
    const live = game(3, { gameState: GameState.Live })
    const me = member([pick(final, final.homeTeam), pick(upcoming, upcoming.homeTeam), pick(live, live.awayTeam)])

    const picks = memberPicksWithPlayers(me, [final, upcoming, live])

    expect(picks.map((p) => [p.game.id, p.player.id])).toEqual([
      [3, 302],
      [2, 201],
      [1, 101]
    ])
  })

  it("leaves out picks of games it isn't given, and players no longer on the roster", () => {
    const shown = game(1)
    const gone = { ...pick(shown, shown.homeTeam), playerId: 999 }

    expect(memberPicksWithPlayers(member([pick(game(2), game(2).homeTeam), gone]), [shown])).toEqual([])
  })
})

describe('membersViewerFirst', () => {
  it('puts the viewer first without reordering the lobby', () => {
    const members = [member([], 'a'), member([], 'me'), member([], 'b')]

    expect(membersViewerFirst(members, 'me').map((m) => m.id)).toEqual(['me', 'a', 'b'])
    expect(members.map((m) => m.id)).toEqual(['a', 'me', 'b'])
  })
})

describe('nextPicksCountdown', () => {
  it("counts down to the soonest of the lobby's games that hasn't opened", () => {
    const soon = game(1, { pickingOpensUtc: new Date('2026-11-22T21:00:00Z') })
    const later = game(2, { pickingOpensUtc: new Date('2026-11-22T23:00:00Z') })
    const notInLobby = game(3, { pickingOpensUtc: new Date('2026-11-22T20:30:00Z') })

    expect(nextPicksCountdown(lobby([1, 2]), [later, notInLobby, soon], now)).toBe(formatCountdown(soon.pickingOpensUtc, now))
  })

  it('is undefined once every game has opened', () => {
    expect(nextPicksCountdown(lobby([1]), [game(1)], now)).toBeUndefined()
  })
})

describe('skatersForPicking', () => {
  it("leaves out goalies and lists the member's picks first", () => {
    const team = { id: 1, roster: [player(1), player(2, 'G'), player(3), player(4, 'g')] } as GameTeam
    const me = member([{ playerId: 3 } as LobbyMemberPick])

    expect(skatersForPicking(team, me).map((p) => p.id)).toEqual([3, 1])
  })
})

describe('firstTeamToPick', () => {
  it('is the away team while it has picks left', () => {
    const g = game(1)

    expect(firstTeamToPick(lobby([1]), member(), g)).toBe(g.awayTeam)
  })

  it("is the home team once the away team's picks are used", () => {
    const g = game(1)

    expect(firstTeamToPick(lobby([1]), member([pick(g, g.awayTeam)]), g)).toBe(g.homeTeam)
  })

  it('is the home team until the rosters load', () => {
    const g = game(1)
    g.homeTeam.roster = []

    expect(firstTeamToPick(lobby([1]), member(), g)).toBe(g.homeTeam)
  })
})

describe('nextPickStep', () => {
  it("goes to the game's other team when it has picks left", () => {
    const g = game(1)
    const me = member([pick(g, g.awayTeam)])

    expect(nextPickStep(lobby([1]), me, g, g.awayTeam, [g], now)).toEqual({ team: g.homeTeam })
  })

  it('goes to the next game with picks left once this one is done', () => {
    const done = game(1)
    const next = game(2)
    const me = member([pick(done, done.awayTeam), pick(done, done.homeTeam)])

    expect(nextPickStep(lobby([1, 2]), me, done, done.homeTeam, [done, next], now)).toEqual({ game: next })
  })

  it('is done when no game has picks left', () => {
    const g = game(1)
    const me = member([pick(g, g.awayTeam), pick(g, g.homeTeam)])

    expect(nextPickStep(lobby([1]), me, g, g.homeTeam, [g], now)).toBe('done')
  })
})
