import { AxiosError, AxiosHeaders } from 'axios'
import { describe, expect, it, vi } from 'vitest'
import { isSessionRejected, restoreSession } from '@/helpers/sessionHelpers'

const httpError = (status: number) =>
  new AxiosError(`Request failed with status code ${status}`, 'ERR_BAD_RESPONSE', undefined, undefined, {
    status,
    statusText: '',
    headers: {},
    config: { headers: new AxiosHeaders() },
    data: {}
  })
const networkError = () => new AxiosError('Network Error', 'ERR_NETWORK')

describe('isSessionRejected', () => {
  it('is true only for a 401 from the server', () => {
    expect(isSessionRejected(httpError(401))).toBe(true)
    expect(isSessionRejected(httpError(500))).toBe(false)
    expect(isSessionRejected(httpError(503))).toBe(false)
    expect(isSessionRejected(networkError())).toBe(false)
    expect(isSessionRejected(new Error('boom'))).toBe(false)
  })
})

describe('restoreSession', () => {
  const noWait = { sleep: () => Promise.resolve() }

  it('returns the session when the refresh works', async () => {
    expect(await restoreSession(async () => 'session', noWait)).toEqual({ ok: true, value: 'session' })
  })

  it('gives up at once when the server rejects the session', async () => {
    const attempt = vi.fn().mockRejectedValue(httpError(401))

    expect(await restoreSession(attempt, noWait)).toEqual({ ok: false, rejected: true })
    expect(attempt).toHaveBeenCalledOnce()
  })

  it('retries while the network or server is down, then succeeds', async () => {
    const attempt = vi.fn().mockRejectedValueOnce(networkError()).mockRejectedValueOnce(httpError(503)).mockResolvedValue('session')

    expect(await restoreSession(attempt, noWait)).toEqual({ ok: true, value: 'session' })
    expect(attempt).toHaveBeenCalledTimes(3)
  })

  it("reports it couldn't reach the server, without rejecting the session, after its retries", async () => {
    const attempt = vi.fn().mockRejectedValue(networkError())
    const sleep = vi.fn().mockResolvedValue(undefined)

    expect(await restoreSession(attempt, { delaysMs: [10, 20], sleep })).toEqual({ ok: false, rejected: false })
    expect(attempt).toHaveBeenCalledTimes(3)
    expect(sleep.mock.calls).toEqual([[10], [20]])
  })
})
