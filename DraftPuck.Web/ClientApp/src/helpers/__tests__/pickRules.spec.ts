import { describe, expect, it } from 'vitest'
import { canPickPlayer, canRemovePick, findPick, getPickableStatus, hasPicksLeftInGame, isPickingOpen, picksRemaining } from '@/helpers/pickRules'
import GamePickableStatus from '@/enums/gamePickableStatus'

const HOME = 1
const AWAY = 2
const opens = new Date('2026-11-13T23:30:00Z')
const beforeOpen = new Date('2026-11-13T23:29:59Z')
const afterOpen = new Date('2026-11-14T01:00:00Z')

function game(id: number, extra: Partial<Game> = {}): Game {
  return {
    id,
    pickingOpensUtc: opens,
    isPickingClosed: false,
    canMembersRemovePicks: true,
    homeTeam: { id: HOME, roster: [{ id: 101 }] },
    awayTeam: { id: AWAY, roster: [{ id: 201 }] },
    ...extra
  } as unknown as Game
}

function pick(memberId: string, gameId: number, teamId: number, playerId: number): LobbyMemberPick {
  return { id: `${memberId}-${playerId}`, lobbyMemberId: memberId, gameId, teamId, playerId } as LobbyMemberPick
}

function member(id: string, picks: LobbyMemberPick[] = []): LobbyMember {
  return { id, picks } as unknown as LobbyMember
}

function lobby(members: LobbyMember[], extra: Partial<Lobby> = {}): Lobby {
  return { gameIds: [10, 20], picksPerTeam: 2, canManageAllPicks: false, members, ...extra } as unknown as Lobby
}

describe('pick window', () => {
  it('opens at the time the server gives, inclusive', () => {
    expect(isPickingOpen(game(10), beforeOpen)).toBe(false)
    expect(isPickingOpen(game(10), opens)).toBe(true)
    expect(isPickingOpen(game(10), afterOpen)).toBe(true) // still open after puck drop
  })

  it('closes when the server says so (the game is final)', () => {
    expect(isPickingOpen(game(10, { isPickingClosed: true }), afterOpen)).toBe(false)
  })
})

describe('picksRemaining', () => {
  it('counts picks per team and per game, minus players selected but not locked in', () => {
    const me = member('me', [pick('me', 10, HOME, 101), pick('me', 20, HOME, 102)])
    const l = lobby([me])

    expect(picksRemaining(l, me, 10, HOME)).toBe(1) // the game-20 pick for the same team id doesn't count here
    expect(picksRemaining(l, me, 10, AWAY)).toBe(2)
    expect(picksRemaining(l, me, 10, AWAY, 1)).toBe(1)
  })
})

describe('findPick', () => {
  it('only looks at the given game', () => {
    const other = member('other', [pick('other', 20, HOME, 101)])
    const l = lobby([member('me'), other])

    expect(findPick(l, 10, 101)).toBeUndefined()
    expect(findPick(l, 20, 101)?.member).toBe(other)
  })
})

describe('canPickPlayer', () => {
  const player = { id: 101, teamId: HOME }

  it('allows an unpicked player in an open lobby game with picks left', () => {
    const me = member('me')
    expect(canPickPlayer(lobby([me]), me, game(10), player, afterOpen)).toBe(true)
  })

  it('refuses before the window opens, once closed, and for games outside the lobby', () => {
    const me = member('me')
    expect(canPickPlayer(lobby([me]), me, game(10), player, beforeOpen)).toBe(false)
    expect(canPickPlayer(lobby([me]), me, game(10, { isPickingClosed: true }), player, afterOpen)).toBe(false)
    expect(canPickPlayer(lobby([me]), me, game(30), player, afterOpen)).toBe(false)
  })

  it('refuses a player someone already has in this game, but not one picked in another game', () => {
    const me = member('me')
    const other = member('other', [pick('other', 10, HOME, 101), pick('other', 20, HOME, 102)])
    const l = lobby([me, other])

    expect(canPickPlayer(l, me, game(10), player, afterOpen)).toBe(false)
    expect(canPickPlayer(l, me, game(10), { id: 102, teamId: HOME }, afterOpen)).toBe(true)
  })

  it('refuses once the team has no picks left, counting the current selection', () => {
    const me = member('me', [pick('me', 10, HOME, 102)])
    expect(canPickPlayer(lobby([me]), me, game(10), player, afterOpen, 0)).toBe(true)
    expect(canPickPlayer(lobby([me]), me, game(10), player, afterOpen, 1)).toBe(false)
  })
})

describe('hasPicksLeftInGame', () => {
  it('is true while either team has picks left', () => {
    const me = member('me', [pick('me', 10, HOME, 101), pick('me', 10, HOME, 102), pick('me', 10, AWAY, 201)])
    expect(hasPicksLeftInGame(lobby([me]), me, game(10), afterOpen)).toBe(true)

    me.picks.push(pick('me', 10, AWAY, 202))
    expect(hasPicksLeftInGame(lobby([me]), me, game(10), afterOpen)).toBe(false)
  })

  it('is false until the rosters have loaded', () => {
    const me = member('me')
    const noRosters = game(10, { homeTeam: { id: HOME, roster: [] }, awayTeam: { id: AWAY, roster: [] } } as unknown as Partial<Game>)
    expect(hasPicksLeftInGame(lobby([me]), me, noRosters, afterOpen)).toBe(false)
  })
})

describe('canRemovePick', () => {
  const me = member('me')
  const mine = pick('me', 10, HOME, 101)
  const theirs = pick('other', 10, HOME, 102)

  it('lets members remove their own picks only while the server allows it', () => {
    expect(canRemovePick(lobby([me]), game(10), mine, me)).toBe(true)
    expect(canRemovePick(lobby([me]), game(10, { canMembersRemovePicks: false }), mine, me)).toBe(false)
    expect(canRemovePick(lobby([me]), game(10), theirs, me)).toBe(false)
  })

  it('lets the host and site admins remove any pick at any time', () => {
    const l = lobby([me], { canManageAllPicks: true })
    expect(canRemovePick(l, game(10, { canMembersRemovePicks: false }), theirs, me)).toBe(true)
  })
})

describe('getPickableStatus', () => {
  const me = member('me')

  it.each([
    ['a game outside the lobby', game(30), afterOpen, GamePickableStatus.Locked],
    ['a final game', game(10, { isPickingClosed: true }), afterOpen, GamePickableStatus.GameComplete],
    ['before picking opens', game(10), beforeOpen, GamePickableStatus.Upcoming],
    ['an open game with picks left', game(10), afterOpen, GamePickableStatus.PicksAvailable]
  ])('%s', (_, g, now, expected) => {
    expect(getPickableStatus(lobby([me]), me, g, now)).toBe(expected)
  })

  it('stays upcoming after picking opens until the rosters load', () => {
    const noRosters = game(10, { homeTeam: { id: HOME, roster: [] }, awayTeam: { id: AWAY, roster: [] } } as unknown as Partial<Game>)
    expect(getPickableStatus(lobby([me]), me, noRosters, afterOpen)).toBe(GamePickableStatus.Upcoming)
  })

  it('shows picks made once every pick is used', () => {
    const full = member('me', [pick('me', 10, HOME, 1), pick('me', 10, HOME, 2), pick('me', 10, AWAY, 3), pick('me', 10, AWAY, 4)])
    expect(getPickableStatus(lobby([full]), full, game(10), afterOpen)).toBe(GamePickableStatus.PicksMade)
  })
})
