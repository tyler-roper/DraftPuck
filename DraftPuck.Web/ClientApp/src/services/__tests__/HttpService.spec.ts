import { afterEach, describe, expect, it, vi } from 'vitest'

const { post } = vi.hoisted(() => ({ post: vi.fn() }))
vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>()
  return { ...actual, default: { ...actual.default, post, create: actual.default.create } }
})
vi.mock('@/services/TokenService', () => ({
  default: { getTokens: () => ({ jwt: null, csrf: 'csrf-1' }), storeTokens: vi.fn(), clearTokens: vi.fn() }
}))

import { refreshTokens } from '@/services/HttpService'

describe('refreshTokens', () => {
  afterEach(() => post.mockReset())

  it('shares one request between refreshes that start together', async () => {
    // The server rotates the token on each use and treats the old one coming back as theft, revoking the session.
    let respond!: (value: unknown) => void
    post.mockReturnValue(new Promise((resolve) => (respond = resolve)))

    const startup = refreshTokens()
    const liveConnection = refreshTokens()
    respond({ data: { jwtToken: 'jwt-2', user: { id: 'u1' } } })

    expect(await startup).toEqual(await liveConnection)
    expect(post).toHaveBeenCalledOnce()
  })

  it('makes a new request once the previous one has finished', async () => {
    post.mockResolvedValue({ data: { jwtToken: 'jwt', user: { id: 'u1' } } })

    await refreshTokens()
    await refreshTokens()

    expect(post).toHaveBeenCalledTimes(2)
  })
})
