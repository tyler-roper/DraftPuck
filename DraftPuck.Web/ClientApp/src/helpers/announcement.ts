/**
 * A one-off announcement on the home page (currently: what's new in the November 2026 update). Dismissing it, or
 * opening it, hides it on this device for good. A new announcement gets a new ID, so it shows even to people who
 * dismissed the last one; past `showUntil` it's gone for everyone, so new players aren't told about "changes".
 */
export const announcement = {
  id: 'whats-new-2026-11',
  showUntil: new Date('2027-01-01T00:00:00Z')
}

const KEY = 'draftpuck:announcementDismissed'

export function isAnnouncementVisible(storage: Storage | undefined = safeStorage(), now: Date = new Date()): boolean {
  if (now >= announcement.showUntil) return false
  try {
    return storage?.getItem(KEY) !== announcement.id
  } catch {
    return true
  }
}

export function dismissAnnouncement(storage: Storage | undefined = safeStorage()) {
  try {
    storage?.setItem(KEY, announcement.id)
  } catch {
    // Storage blocked (private mode): it's hidden until the page reloads.
  }
}

function safeStorage(): Storage | undefined {
  try {
    return window.localStorage
  } catch {
    return undefined
  }
}
