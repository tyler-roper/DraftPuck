import GameState from '@/enums/gameState'
import { formatCountdown } from '@/helpers/dateHelpers'
import { hasPicksLeftInGame, isGameInLobby, picksRemaining } from '@/helpers/pickRules'

/*
 * What the phone's picks panel shows and where it goes next. The rules themselves come from pickRules.ts.
 */

export interface PlayerInGame {
  game: Game
  player: Player
}

/** A member's picks with their players, live games first, then upcoming ones, then the rest. Picks of games not shown are left out. */
export function memberPicksWithPlayers(member: LobbyMember, games: Game[]): PlayerInGame[] {
  const rank = (game: Game) => (game.gameState === GameState.Live ? 0 : game.gameState === GameState.Upcoming ? 1 : 2)

  return member.picks
    .flatMap((pick) => {
      const game = games.find((g) => g.id === pick.gameId)
      const player = game && [...game.homeTeam.roster, ...game.awayTeam.roster].find((p) => p.id === pick.playerId)
      return game && player ? [{ game, player }] : []
    })
    .sort((a, b) => rank(a.game) - rank(b.game))
}

/** The lobby's members with the viewer first, for the member menu. The lobby's own list is left as it is. */
export function membersViewerFirst(members: LobbyMember[], viewerId: string): LobbyMember[] {
  return [...members].sort((a, b) => Number(b.id === viewerId) - Number(a.id === viewerId))
}

/** How long until picking opens for the lobby's next game ("1 hour 57 minutes"), or undefined if no more will open. */
export function nextPicksCountdown(lobby: Lobby, games: Game[], now: Date): string | undefined {
  const next = games
    .filter((game) => isGameInLobby(lobby, game) && game.pickingOpensUtc > now)
    .reduce<Game | undefined>((soonest, game) => (!soonest || game.pickingOpensUtc < soonest.pickingOpensUtc ? game : soonest), undefined)
  return next && formatCountdown(next.pickingOpensUtc, now)
}

/** A team's skaters to pick from (no goalies), the member's own picks first. */
export function skatersForPicking(team: GameTeam, member: LobbyMember): Player[] {
  const picked = new Set(member.picks.map((p) => p.playerId))
  return team.roster.filter((p) => p.position.toLowerCase() !== 'g').sort((a, b) => Number(picked.has(b.id)) - Number(picked.has(a.id)))
}

/** The team to show first when a game is opened: away, unless its picks are used up (or the rosters haven't loaded). */
export function firstTeamToPick(lobby: Lobby, member: LobbyMember, game: Game): GameTeam {
  const awayHasPicksLeft = picksRemaining(lobby, member, game.id, game.awayTeam.id) > 0
  return game.homeTeam.roster.length && awayHasPicksLeft ? game.awayTeam : game.homeTeam
}

export type NextPickStep = { team: GameTeam } | { game: Game } | 'done'

/**
 * Where the panel goes once a team's picks are all locked in: the game's other team if it has picks left, else the
 * next game with picks left, else back to the member's picks ('done').
 */
export function nextPickStep(lobby: Lobby, member: LobbyMember, game: Game, team: GameTeam, games: Game[], now: Date): NextPickStep {
  if (hasPicksLeftInGame(lobby, member, game, now)) return { team: team.id === game.awayTeam.id ? game.homeTeam : game.awayTeam }

  const nextGame = games.find((g) => hasPicksLeftInGame(lobby, member, g, now))
  return nextGame ? { game: nextGame } : 'done'
}
