/** A logo shown instead of the usual one during an event, as the admin page lists it. */
export interface AdminEventLogo {
  id: string
  name: string
  startsUtc: Date
  endsUtc: Date
  logoUrl: string
  wideLogoUrl: string
}

/** The add form. Starts and ends are datetime-local values: the admin's own time zone. */
export interface EventLogoForm {
  name: string
  starts: string
  ends: string
  logoUrl: string
  wideLogoUrl: string
}

export interface CreateEventLogoRequest {
  name: string
  startsUtc: string
  endsUtc: string
  logoUrl: string
  wideLogoUrl: string
}

export type EventLogoStatus = 'On now' | 'Upcoming' | 'Ended'

/** Shown from the start (inclusive) until the end (exclusive), like the server. */
export function eventLogoStatus(logo: Pick<AdminEventLogo, 'startsUtc' | 'endsUtc'>, now: Date): EventLogoStatus {
  if (now < logo.startsUtc) return 'Upcoming'
  return now < logo.endsUtc ? 'On now' : 'Ended'
}

/** A path on this site ("/img/logo-event.png") or a full http(s) URL. */
export function isLogoUrl(url: string): boolean {
  const value = url.trim()
  if (value.startsWith('/')) return !value.startsWith('//')
  return /^https?:\/\/[^/\s]+/i.test(value)
}

/**
 * What's wrong with the form, for quick feedback before sending; null if nothing. The server checks the same things
 * and has the last word.
 */
export function eventLogoFormError(form: EventLogoForm): string | null {
  if (!form.name.trim()) return 'Give the event a name.'
  if (!form.starts || !form.ends) return 'Pick when the event starts and ends.'
  if (new Date(form.ends) <= new Date(form.starts)) return 'The event has to end after it starts.'
  if (!isLogoUrl(form.logoUrl)) return 'The logo must be a path on this site (like /img/logo-event.png) or a full URL.'
  if (!isLogoUrl(form.wideLogoUrl)) return 'The wide logo must be a path on this site (like /img/logo-wide-event.png) or a full URL.'
  return null
}

/** The form as the API takes it: local times converted to UTC. */
export function toCreateEventLogoRequest(form: EventLogoForm): CreateEventLogoRequest {
  return {
    name: form.name.trim(),
    startsUtc: new Date(form.starts).toISOString(),
    endsUtc: new Date(form.ends).toISOString(),
    logoUrl: form.logoUrl.trim(),
    wideLogoUrl: form.wideLogoUrl.trim()
  }
}
