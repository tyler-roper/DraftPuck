import parseISO from 'date-fns/parseISO'

const isoDateFormat = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d*)?(?:[-+]\d{2}:?\d{2}|Z)?$/

export function isIsoDateString(value: object): boolean {
  return value && typeof value === 'string' && isoDateFormat.test(value)
}

export function parseAllDates(body: { [key: string]: any }) {
  if (body === null || body === undefined || typeof body !== 'object') return body

  for (const key of Object.keys(body)) {
    let value = body[key]
    if (isIsoDateString(value)) {
      if (!value.endsWith('Z')) value += 'Z'
      body[key] = parseISO(value)
    } else if (typeof value === 'object') parseAllDates(value)
  }
}
