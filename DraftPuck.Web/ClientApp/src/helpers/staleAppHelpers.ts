/**
 * Each page's code is a separate file with a hashed name, loaded the first time the page is opened. A deploy replaces
 * those files, so a tab (or installed app) opened before it asks for names that no longer exist, and the navigation
 * silently fails. Reloading at the page the user was going to gets the new app.
 */

const LAST_RELOAD_KEY = 'draftpuck:stale-app-reload'
/** Don't reload again within this window: if the files are really missing, a reload loop would make it worse. */
export const RELOAD_GUARD_MS = 60_000

/** Whether the error is a page's code failing to load (the messages browsers and Vite use for it). */
export function isStaleAppError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error ?? '')
  return /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|Unable to preload CSS|Failed to load module script/i.test(
    message
  )
}

interface ReloadDeps {
  storage: Pick<Storage, 'getItem' | 'setItem'> | undefined
  now: number
  navigate: (path: string) => void
}

/** Reloads the app at `path`, unless it already did within the last minute. Returns whether it reloaded. */
export function reloadForNewVersion(path: string, { storage, now, navigate }: ReloadDeps): boolean {
  let last = 0
  try {
    last = Number(storage?.getItem(LAST_RELOAD_KEY) ?? 0)
  } catch {
    // Storage can be unavailable (private mode); reload anyway, the guard is only a safety net.
  }
  if (now - last < RELOAD_GUARD_MS) return false

  try {
    storage?.setItem(LAST_RELOAD_KEY, String(now))
  } catch {
    // As above.
  }
  navigate(path)
  return true
}

/** The real browser dependencies for {@link reloadForNewVersion}. */
export function browserReloadDeps(): ReloadDeps {
  let storage: Storage | undefined
  try {
    storage = window.sessionStorage
  } catch {
    storage = undefined
  }
  return { storage, now: Date.now(), navigate: (path) => window.location.assign(path) }
}
