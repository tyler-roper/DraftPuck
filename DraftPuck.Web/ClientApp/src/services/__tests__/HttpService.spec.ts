import { beforeEach, describe, expect, it, vi } from 'vitest'
import axios from 'axios'
import { getValidJwt } from '@/services/HttpService'
import TokenService from '@/services/TokenService'

vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>()
  return { ...actual, default: { ...actual.default, post: vi.fn(), create: actual.default.create } }
})

function jwtExpiringIn(seconds: number) {
  const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + seconds }))
  return `header.${payload}.signature`
}

function refreshResponse(jwtToken: string) {
  return { data: { jwtToken, antiCsrfToken: 'new-csrf', user: {} } }
}

describe('getValidJwt', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.mocked(axios.post).mockReset()
  })

  it('returns the stored token while it is still valid', async () => {
    const jwt = jwtExpiringIn(600)
    TokenService.storeTokens({ jwtToken: jwt, antiCsrfToken: 'csrf' } as AuthResponse)

    expect(await getValidJwt()).toBe(jwt)
    expect(axios.post).not.toHaveBeenCalled()
  })

  it('refreshes a token that is about to expire, sending the CSRF header', async () => {
    TokenService.storeTokens({ jwtToken: jwtExpiringIn(30), antiCsrfToken: 'csrf' } as AuthResponse)
    vi.mocked(axios.post).mockResolvedValue(refreshResponse('fresh'))

    expect(await getValidJwt()).toBe('fresh')
    expect(axios.post).toHaveBeenCalledWith('/api/auth/refreshTokens/use', undefined, { headers: { 'CSRF-Token': 'csrf' } })
    expect(TokenService.getTokens()).toEqual({ jwt: 'fresh', csrf: 'new-csrf' })
  })

  it('shares one refresh between concurrent callers', async () => {
    // A second refresh with the same (now rotated) cookie would look like token reuse and end the session.
    vi.mocked(axios.post).mockResolvedValue(refreshResponse('fresh'))

    const tokens = await Promise.all([getValidJwt(), getValidJwt(), getValidJwt()])

    expect(tokens).toEqual(['fresh', 'fresh', 'fresh'])
    expect(axios.post).toHaveBeenCalledTimes(1)
  })

  it('treats a malformed token as expired', async () => {
    TokenService.storeTokens({ jwtToken: 'not-a-jwt', antiCsrfToken: 'csrf' } as AuthResponse)
    vi.mocked(axios.post).mockResolvedValue(refreshResponse('fresh'))

    expect(await getValidJwt()).toBe('fresh')
  })
})
