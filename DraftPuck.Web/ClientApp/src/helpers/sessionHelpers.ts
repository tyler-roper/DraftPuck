import { isAxiosError } from 'axios'

/** Whether a failed refresh means the server rejected the session, as opposed to the request not getting through. */
export function isSessionRejected(error: unknown): boolean {
  return isAxiosError(error) && error.response?.status === 401
}

export type RestoreResult<T> = { ok: true; value: T } | { ok: false; rejected: boolean }

export interface RestoreOptions {
  /** Waits between attempts when the server can't be reached. */
  delaysMs?: number[]
  sleep?: (ms: number) => Promise<void>
}

/**
 * Restores the signed-in session. A rejection (no session, or an expired or revoked one) ends it at once. A network or
 * server error is retried: the app often opens before the network is up (an installed app woken by a notification),
 * or mid-deploy, and treating that as "signed out" used to replace the user's session with a new guest.
 */
export async function restoreSession<T>(
  attempt: () => Promise<T>,
  { delaysMs = [1000, 2000, 4000, 8000], sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms)) }: RestoreOptions = {}
): Promise<RestoreResult<T>> {
  for (let i = 0; ; i++) {
    try {
      return { ok: true, value: await attempt() }
    } catch (error) {
      if (isSessionRejected(error)) return { ok: false, rejected: true }
      if (i >= delaysMs.length) return { ok: false, rejected: false }
      await sleep(delaysMs[i])
    }
  }
}
