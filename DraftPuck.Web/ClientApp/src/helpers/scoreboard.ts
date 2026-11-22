import GameState from '@/enums/gameState'
import PlayType from '@/enums/playType'
import { getOrdinal } from '@/helpers/gameHelpers'

/** A period column on the scoreboard: always 1 to 3, plus any overtime or shootout played. */
export interface LinescoreColumn {
  number: number
  label: string
  // Whether it has been reached, so its goals show.
  isReached: boolean
}

export function linescoreColumns(game: Game): LinescoreColumn[] {
  const columns = game.goalsByPeriod.map((period) => ({
    number: period.number,
    label: period.number > 3 ? (getOrdinal(period.number, period.periodType) ?? `${period.number}`) : `${period.number}`,
    isReached: period.number <= game.period
  }))
  for (let number = columns.length + 1; number <= 3; number++) columns.push({ number, label: `${number}`, isReached: false })
  return columns
}

/** A team's goals in one period. */
export function goalsInPeriod(game: Game, team: GameTeam, period: number): number {
  const summary = game.goalsByPeriod.find((p) => p.number === period)
  if (!summary) return 0
  return team.id === game.homeTeam.id ? summary.homeGoals : summary.awayGoals
}

/** A team's scorers in the order they first scored, with a count for more than one: "S Aho, J Blake (2)". */
export function scorerSummary(game: Game, team: GameTeam): string {
  const goalsByScorer = new Map<number, number>()
  for (const play of game.plays)
    if (play.type === PlayType.Goal && play.primaryTeamId === team.id && play.primaryPlayerId != null)
      goalsByScorer.set(play.primaryPlayerId, (goalsByScorer.get(play.primaryPlayerId) ?? 0) + 1)

  return [...goalsByScorer]
    .flatMap(([playerId, goals]) => {
      const player = team.roster.find((p) => p.id === playerId)
      if (!player) return []
      const name = `${player.firstName[0]} ${player.lastName}`
      return [goals > 1 ? `${name} (${goals})` : name]
    })
    .join(', ')
}

/** The skater advantage, as the situation badge shows it: "PP" for 5-on-4, else "5-on-3", "4-on-3" and so on. */
export function powerPlayLabel(game: Game): string {
  const [home, away] = [game.homeTeam.strength, game.awayTeam.strength]
  const label = `${Math.max(home, away)}-on-${Math.min(home, away)}`
  return label === '5-on-4' ? 'PP' : label
}

/** Overtime, or the last five minutes of a one-goal (or tied) third period: the scoreboard pulses. */
export function isCriticalMoment(game: Game): boolean {
  if (game.gameState !== GameState.Live) return false
  if (game.period > 3) return true
  const isClose = Math.abs(game.awayTeam.score - game.homeTeam.score) <= 1
  return game.period === 3 && isClose && game.minutesRemainingInPeriod < 5
}

/** "LW" and "RW" for wingers; other positions as they are. */
export function friendlyPosition(position: string): string {
  return position === 'L' || position === 'R' ? `${position}W` : position
}

/** Who picked each player in this game, by player ID. */
export function pickerNames(lobby: Lobby, gameId: number): Map<number, string> {
  return new Map(lobby.members.flatMap((m) => m.picks.filter((p) => p.gameId === gameId).map((p) => [p.playerId, m.name] as const)))
}
