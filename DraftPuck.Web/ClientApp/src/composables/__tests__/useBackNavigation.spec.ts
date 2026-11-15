import { afterEach, describe, expect, it, vi } from 'vitest'

const router = { back: vi.fn(), replace: vi.fn() }
vi.mock('vue-router', () => ({ useRouter: () => router }))

import { hasInAppHistory, useBackNavigation } from '@/composables/useBackNavigation'

describe('hasInAppHistory', () => {
  it.each([
    [{ back: '/lobbies', current: '/lobby/ABCD' }, true],
    [{ back: null, current: '/lobby/ABCD/review' }, false],
    [null, false],
    [undefined, false],
    ['not a state object', false]
  ])('%j -> %s', (state, expected) => {
    expect(hasInAppHistory(state)).toBe(expected)
  })
})

describe('useBackNavigation', () => {
  afterEach(() => {
    window.history.replaceState(null, '')
    vi.clearAllMocks()
  })

  it('goes back when the app navigated here', () => {
    window.history.replaceState({ back: '/u/sam', current: '/account' }, '')

    useBackNavigation(() => '/')()

    expect(router.back).toHaveBeenCalledOnce()
    expect(router.replace).not.toHaveBeenCalled()
  })

  it('goes to the fallback when the page was opened directly', () => {
    // The first page of a visit, e.g. a lobby's summary opened from a link or the installed app.
    window.history.replaceState({ back: null, current: '/lobby/ABCD/review' }, '')

    useBackNavigation(() => '/u/sam')()

    expect(router.replace).toHaveBeenCalledWith('/u/sam')
    expect(router.back).not.toHaveBeenCalled()
  })
})
