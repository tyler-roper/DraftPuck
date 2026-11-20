/**
 * Applies a poll's changes to the copy of the game they were made from, in place (anything holding the game keeps
 * seeing its latest state). Returns false, changing nothing, when they can't be applied to this copy: it isn't the
 * one they were made from, or a play they keep isn't held. The caller then fetches the whole game.
 */
export function applyGameChanges(game: Game, changes: GameChanges): boolean {
  if (changes.baseUpdatedUtc && changes.baseUpdatedUtc.getTime() !== game.updatedUtc.getTime()) return false

  const plays = new Map(game.plays.map((p) => [p.id, p]))
  for (const play of changes.plays) plays.set(play.id, play)
  const ordered = changes.playIds.map((id) => plays.get(id))
  if (ordered.some((p) => p === undefined)) return false

  game.updatedUtc = changes.updatedUtc
  game.dateTime = changes.dateTime
  game.gameType = changes.gameType
  game.gameState = changes.gameState
  game.period = changes.period
  game.periodType = changes.periodType
  game.minutesRemainingInPeriod = changes.minutesRemainingInPeriod
  game.secondsRemainingInPeriod = changes.secondsRemainingInPeriod
  game.timeRemainingInPeriod = changes.timeRemainingInPeriod
  game.goalsByPeriod = changes.goalsByPeriod
  game.pickingOpensUtc = changes.pickingOpensUtc
  game.isPickingClosed = changes.isPickingClosed
  game.canMembersRemovePicks = changes.canMembersRemovePicks
  game.plays = ordered as Play[]
  applyTeamChanges(game.homeTeam, changes.homeTeam)
  applyTeamChanges(game.awayTeam, changes.awayTeam)
  if (changes.playerSummaries) game.playerSummaries = changes.playerSummaries
  if (changes.formerPlayers) game.formerPlayers = changes.formerPlayers
  return true
}

function applyTeamChanges(team: GameTeam, changes: GameTeamChanges) {
  team.score = changes.score
  team.situations = changes.situations
  team.strength = changes.strength
  if (changes.roster) team.roster = changes.roster
}
