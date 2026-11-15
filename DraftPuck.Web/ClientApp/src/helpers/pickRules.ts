import GamePickableStatus from '@/enums/gamePickableStatus'

/*
 * The pick rules, as the UI applies them. The server enforces every rule; this module exists so the UI agrees with it.
 * Rules about timing, closing and who may remove picks are facts the server sends with each game and lobby
 * (pickingOpensUtc, isPickingClosed, canMembersRemovePicks, canManageAllPicks), so they're never re-implemented here.
 * What's left is combining those facts with the clock and the lobby's picks (which are only active picks).
 */

export const isGameInLobby = (lobby: Lobby, game: Game) => lobby.gameIds.includes(game.id)

/** Opens 30 minutes before puck drop (per the server) and stays open until the game is final. */
export const isPickingOpen = (game: Game, now: Date) => now >= game.pickingOpensUtc && !game.isPickingClosed

/** A member's picks left for one team in one game, after any players they've selected but not locked in yet. */
export function picksRemaining(lobby: Lobby, member: LobbyMember, gameId: number, teamId: number, selectedCount = 0) {
  const used = member.picks.filter((p) => p.gameId === gameId && p.teamId === teamId).length
  return lobby.picksPerTeam - used - selectedCount
}

/** Who in the lobby has this player in this game, if anyone. A player can be picked by one member per game. */
export function findPick(lobby: Lobby, gameId: number, playerId: number): { pick: LobbyMemberPick; member: LobbyMember } | undefined {
  for (const member of lobby.members) {
    const pick = member.picks.find((p) => p.gameId === gameId && p.playerId === playerId)
    if (pick) return { pick, member }
  }
}

/** Whether the member could pick (or select) this player now, given how many players they've already selected for its team. */
export function canPickPlayer(lobby: Lobby, member: LobbyMember, game: Game, player: { id: number; teamId: number }, now: Date, selectedCount = 0) {
  return (
    isGameInLobby(lobby, game) &&
    isPickingOpen(game, now) &&
    !findPick(lobby, game.id, player.id) &&
    picksRemaining(lobby, member, game.id, player.teamId, selectedCount) > 0
  )
}

/** Whether the member has anything left to pick in this game: it's open, its rosters have loaded, and a team has picks left. */
export function hasPicksLeftInGame(lobby: Lobby, member: LobbyMember, game: Game, now: Date) {
  return (
    isGameInLobby(lobby, game) &&
    isPickingOpen(game, now) &&
    areRostersLoaded(game) &&
    [game.homeTeam, game.awayTeam].some((team) => picksRemaining(lobby, member, game.id, team.id) > 0)
  )
}

/** The host and site admins can remove any pick at any time; members can remove their own until puck drop. */
export function canRemovePick(lobby: Lobby, game: Game, pick: LobbyMemberPick, viewer: LobbyMember) {
  return lobby.canManageAllPicks || (pick.lobbyMemberId === viewer.id && game.canMembersRemovePicks)
}

/** What the picks panel and the scores ribbon show for a game. Until its rosters load there's nothing to pick, so it's still upcoming. */
export function getPickableStatus(lobby: Lobby, member: LobbyMember, game: Game, now: Date): GamePickableStatus {
  if (!isGameInLobby(lobby, game)) return GamePickableStatus.Locked
  if (game.isPickingClosed) return GamePickableStatus.GameComplete
  if (now < game.pickingOpensUtc || !areRostersLoaded(game)) return GamePickableStatus.Upcoming
  return hasPicksLeftInGame(lobby, member, game, now) ? GamePickableStatus.PicksAvailable : GamePickableStatus.PicksMade
}

const areRostersLoaded = (game: Game) => game.homeTeam.roster.length + game.awayTeam.roster.length > 0
