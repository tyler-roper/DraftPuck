import { describe, expect, it } from 'vitest'
import { START_LOCATION, type RouteLocationNormalized } from 'vue-router'
import { isShowingFirstPage, recordNavigation } from '@/helpers/routerHelpers'

const home = { name: 'Home', path: '/' } as RouteLocationNormalized

describe('isShowingFirstPage', () => {
  it('is the page the app opened on, until the player moves to another', () => {
    recordNavigation(START_LOCATION)
    expect(isShowingFirstPage()).toBe(true)

    recordNavigation(home)
    expect(isShowingFirstPage()).toBe(false)
  })
})
