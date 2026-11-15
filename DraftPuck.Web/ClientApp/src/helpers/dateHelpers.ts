import parseISO from 'date-fns/parseISO'

const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/

export function isIsoDateString(value: unknown): value is string {
  return typeof value === 'string' && isoDateFormat.test(value)
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
