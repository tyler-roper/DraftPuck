import { differenceInCalendarDays, formatDuration, intervalToDuration, parseISO } from 'date-fns'

const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/

export function isIsoDateString(value: unknown): value is string {
  return typeof value === 'string' && isoDateFormat.test(value)
}

/**
 * The time between two moments, in either order, as a countdown label: "45 seconds" under a minute, otherwise days,
 * hours and minutes ("1 day 2 hours 1 minute") without the seconds.
 */
export function formatCountdown(from: Date, to: Date): string {
  // intervalToDuration gives negative units for a reversed interval and leaves out the zero ones.
  const [start, end] = from <= to ? [from, to] : [to, from]
  const duration = intervalToDuration({ start, end })

  const underAMinute = !duration.years && !duration.months && !duration.days && !duration.hours && !duration.minutes
  return underAMinute
    ? formatDuration({ seconds: duration.seconds ?? 0 }, { zero: true })
    : formatDuration(duration, { format: ['days', 'hours', 'minutes'] })
}

/** Turns every ISO date string in a parsed JSON body into a Date, in place. Strings without a Z are read as UTC. */
export function parseAllDates(body: unknown): void {
  if (body === null || typeof body !== 'object') return

  const record = body as Record<string, unknown>
  for (const key of Object.keys(record)) {
    const value = record[key]
    if (isIsoDateString(value)) record[key] = parseISO(value.endsWith('Z') ? value : value + 'Z')
    else if (typeof value === 'object') parseAllDates(value)
  }
}

/**
 * How long ago a day was, by calendar day: "Today" (or the label given, like "Tonight"), "Yesterday", then "3d ago",
 * "2w ago", "5mo ago" and "1y ago". Months are "mo", so they aren't read as minutes.
 */
export function timeAgo(date: Date, now: Date = new Date(), today = 'Today'): string {
  const days = differenceInCalendarDays(now, date)
  if (days <= 0) return today
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days}d ago`
  if (days < 30) return `${Math.floor(days / 7)}w ago`
  if (days < 365) return `${Math.min(11, Math.floor(days / 30))}mo ago`
  return `${Math.floor(days / 365)}y ago`
}
