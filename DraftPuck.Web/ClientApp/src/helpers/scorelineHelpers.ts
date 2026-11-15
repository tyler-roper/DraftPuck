/** A goal's score, leader first, and which number is the scoring team's (null if the scorer's team is unknown). */
export interface Scoreline {
  first: number
  second: number
  highlighted: 'first' | 'second' | null
}

/**
 * The score after a goal, written leader first ("5-4") with the scoring team's number highlighted: first if the goal
 * kept or put them ahead, second if they still trail or just tied it ("caught up").
 */
export function goalScoreline(goal: { primaryTeamId: number | null; homeScore: number; awayScore: number }, homeTeamId: number): Scoreline {
  const high = Math.max(goal.homeScore, goal.awayScore)
  const low = Math.min(goal.homeScore, goal.awayScore)
  if (goal.primaryTeamId == null) return { first: high, second: low, highlighted: null }

  const scorerScore = goal.primaryTeamId === homeTeamId ? goal.homeScore : goal.awayScore
  const otherScore = goal.primaryTeamId === homeTeamId ? goal.awayScore : goal.homeScore

  return scorerScore > otherScore
    ? { first: scorerScore, second: otherScore, highlighted: 'first' }
    : { first: otherScore, second: scorerScore, highlighted: 'second' }
}
