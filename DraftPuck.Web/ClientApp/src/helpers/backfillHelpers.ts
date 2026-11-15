/** An achievement backfill run, as the admin API returns it. */
export interface AchievementBackfill {
  id: string
  startedUtc: string | Date // HttpService turns ISO dates into Dates
  usersQueued: number
  usersChecked: number
  achievementsAwarded: number
  isComplete: boolean
}

function plural(count: number, word: string): string {
  return `${count.toLocaleString()} ${word}${count === 1 ? '' : 's'}`
}

/** How far through the run is, 0 to 100. */
export function backfillProgress(run: AchievementBackfill): number {
  if (run.usersQueued === 0) return 100
  return Math.min(100, Math.round((run.usersChecked / run.usersQueued) * 100))
}

/** One line for the admin page: progress while it runs, the result once it's done. */
export function describeBackfill(run: AchievementBackfill): string {
  const awarded = plural(run.achievementsAwarded, 'achievement')
  if (run.isComplete) return `Done: ${plural(run.usersChecked, 'player')} checked, ${awarded} awarded.`
  return `Checking… ${run.usersChecked.toLocaleString()} of ${plural(run.usersQueued, 'player')}, ${awarded} awarded so far.`
}
