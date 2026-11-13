import { describe, expect, it } from 'vitest'
import { getForegroundToast } from '@/helpers/pushHelpers'

const picksReady = {
  type: 'LobbyEvent',
  title: 'Time to pick players!',
  body: 'One or more games is about to start! Make your picks now.',
  link: 'https://draftpuck.com/lobby/AEOP'
}

describe('getForegroundToast', () => {
  it('shows lobby pushes that arrive while the app is open elsewhere', () => {
    // Regression: these were dropped, because Firebase gives foreground pushes to the page, not the service worker.
    expect(getForegroundToast(picksReady, '/')).toEqual({
      title: 'Time to pick players!',
      body: 'One or more games is about to start! Make your picks now.',
      path: '/lobby/AEOP'
    })
  })

  it('stays quiet for events in the lobby being viewed, which updates live', () => {
    expect(getForegroundToast(picksReady, '/lobby/AEOP')).toBeNull()
    expect(getForegroundToast(picksReady, '/lobby/aeop/')).toBeNull()
  })

  it('still shows events from a different lobby', () => {
    expect(getForegroundToast(picksReady, '/lobby/ZZZZ')?.path).toBe('/lobby/AEOP')
  })

  it('shows pushes without a link, with nothing to open', () => {
    const toast = getForegroundToast({ type: 'LobbyEvent', title: 'Hello', body: 'World' }, '/')

    expect(toast).toEqual({ title: 'Hello', body: 'World', path: undefined })
  })

  it('ignores pushes with no title', () => {
    expect(getForegroundToast({ type: 'LobbyEvent', body: 'x' }, '/')).toBeNull()
    expect(getForegroundToast(undefined, '/')).toBeNull()
  })
})
