import { describe, expect, it, vi } from 'vitest'
import { isStaleAppError, RELOAD_GUARD_MS, reloadForNewVersion } from '@/helpers/staleAppHelpers'

describe('isStaleAppError', () => {
  it.each([
    // Chrome, Firefox and Safari word it differently; Vite reports missing CSS separately.
    'Failed to fetch dynamically imported module: https://draftpuck.com/LoginView-B3xT.js',
    'error loading dynamically imported module: https://draftpuck.com/LoginView-B3xT.js',
    'Importing a module script failed.',
    'Unable to preload CSS for /LobbyView-x1.css'
  ])('recognises "%s"', (message) => {
    expect(isStaleAppError(new Error(message))).toBe(true)
  })

  it.each([new Error('Request failed with status code 500'), new Error('Navigation cancelled'), null, undefined])(
    'ignores unrelated errors (%s)',
    (error) => {
      expect(isStaleAppError(error)).toBe(false)
    }
  )
})

describe('reloadForNewVersion', () => {
  const memoryStorage = () => {
    const values = new Map<string, string>()
    return { getItem: (k: string) => values.get(k) ?? null, setItem: (k: string, v: string) => void values.set(k, v) }
  }

  it('reloads at the page the user was going to', () => {
    const navigate = vi.fn()

    expect(reloadForNewVersion('/login', { storage: memoryStorage(), now: 1_000_000, navigate })).toBe(true)
    expect(navigate).toHaveBeenCalledWith('/login')
  })

  it("doesn't reload again within a minute, so a real outage can't loop", () => {
    const storage = memoryStorage()
    const navigate = vi.fn()

    reloadForNewVersion('/login', { storage, now: 1_000_000, navigate })
    expect(reloadForNewVersion('/login', { storage, now: 1_000_000 + RELOAD_GUARD_MS - 1, navigate })).toBe(false)
    expect(reloadForNewVersion('/login', { storage, now: 1_000_000 + RELOAD_GUARD_MS, navigate })).toBe(true)
    expect(navigate).toHaveBeenCalledTimes(2)
  })

  it('still reloads when storage is unavailable', () => {
    const navigate = vi.fn()
    const broken = {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      }
    }

    expect(reloadForNewVersion('/u/sam', { storage: broken, now: 1_000_000, navigate })).toBe(true)
    expect(navigate).toHaveBeenCalledWith('/u/sam')
  })
})
