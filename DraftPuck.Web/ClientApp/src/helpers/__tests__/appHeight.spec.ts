import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

// public/app-height.js runs as a plain script before the app loads; run the real file against a fake visual viewport.
// (Tests run from ClientApp, as npm test does.)
const script = readFileSync(resolve(process.cwd(), 'public/app-height.js'), 'utf8')

class FakeVisualViewport extends EventTarget {
  height = 800
  scale = 1
  pageTop = 0
}

let vv: FakeVisualViewport
const appTop = () => document.documentElement.style.getPropertyValue('--app-top')
const appHeight = () => document.documentElement.style.getPropertyValue('--app-height')

function field(type: string) {
  const input = document.createElement('input')
  input.type = type
  document.body.appendChild(input)
  return input
}

describe('app-height.js', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vv = new FakeVisualViewport()
    Object.defineProperty(window, 'visualViewport', { value: vv, configurable: true })
    window.scrollTo = vi.fn() as unknown as typeof window.scrollTo
    document.body.innerHTML = ''
    new Function(script)()
  })

  afterEach(() => {
    vi.useRealTimers()
    ;(document.activeElement as HTMLElement | null)?.blur()
  })

  it('sizes the app to the visible screen', () => {
    vv.height = 640
    vv.dispatchEvent(new Event('resize'))

    expect(appHeight()).toBe('640px')
  })

  it('keeps the app at the top when the page scrolls or bounces, and scrolls the page back', () => {
    Object.defineProperty(window, 'scrollY', { value: 300, configurable: true })
    vv.pageTop = 300
    vv.dispatchEvent(new Event('scroll'))

    expect(appTop()).toBe('0px')
    expect(window.scrollTo).toHaveBeenCalledWith(0, 0)
    Object.defineProperty(window, 'scrollY', { value: 0, configurable: true })
  })

  it('follows the keyboard pan while typing, and returns to the top after', () => {
    const chat = field('text')
    chat.focus()
    vv.height = 420
    vv.pageTop = 280
    vv.dispatchEvent(new Event('scroll'))
    expect(appTop()).toBe('280px')

    chat.blur()
    vv.pageTop = 0
    window.dispatchEvent(new Event('focusout'))
    expect(appTop()).toBe('0px')
  })

  it("doesn't treat a focused checkbox or button as typing", () => {
    field('checkbox').focus()
    vv.pageTop = 120
    vv.dispatchEvent(new Event('scroll'))

    expect(appTop()).toBe('0px')
  })
})
