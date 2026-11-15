import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DEFAULT_TIMEOUT_MS, MAX_TOASTS, toast, useToastStack } from '@/composables/useToast'

const { toasts, dismiss, pause, resume } = useToastStack()

describe('useToast', () => {
  beforeEach(() => vi.useFakeTimers())

  afterEach(() => {
    for (const { id } of [...toasts]) dismiss(id)
    vi.useRealTimers()
  })

  it('adds toasts of each type as plain text', () => {
    toast.success('Saved')
    toast.error('<b>Nope</b>')
    toast.show('Waiting')

    expect(toasts.map((t) => [t.type, t.message, t.isHtml])).toEqual([
      ['success', 'Saved', false],
      ['error', '<b>Nope</b>', false],
      ['default', 'Waiting', false]
    ])
  })

  it('marks html toasts and keeps their title and options', () => {
    const onClick = vi.fn()
    toast.html('<b>Goal</b>', { type: 'info', title: 'GOAL!', icon: 'fi fi-sr-bell', onClick })

    expect(toasts[0]).toMatchObject({ type: 'info', message: '<b>Goal</b>', isHtml: true, title: 'GOAL!', icon: 'fi fi-sr-bell' })
    expect(toasts[0].onClick).toBe(onClick)
  })

  it('closes a toast after its timeout, five seconds by default', () => {
    toast.show('Default')
    toast.success('Quick', { timeout: 2000 })

    vi.advanceTimersByTime(2000)
    expect(toasts.map((t) => t.message)).toEqual(['Default'])

    vi.advanceTimersByTime(DEFAULT_TIMEOUT_MS - 2000)
    expect(toasts).toHaveLength(0)
  })

  it('keeps a toast without a timeout until it is dismissed', () => {
    const id = toast.show('Waiting on permissions...', { timeout: false })

    vi.advanceTimersByTime(60_000)
    expect(toasts).toHaveLength(1)

    dismiss(id)
    expect(toasts).toHaveLength(0)
  })

  it('stops counting down while paused and finishes the rest after resuming', () => {
    const id = toast.show('Hovered', { timeout: 3000 })

    vi.advanceTimersByTime(1000)
    pause(id)
    vi.advanceTimersByTime(10_000)
    expect(toasts).toHaveLength(1)

    resume(id)
    vi.advanceTimersByTime(1999)
    expect(toasts).toHaveLength(1)
    vi.advanceTimersByTime(1)
    expect(toasts).toHaveLength(0)
  })

  it('closes the oldest toasts beyond the limit', () => {
    for (let i = 1; i <= MAX_TOASTS + 2; i++) toast.show(`Toast ${i}`)

    expect(toasts).toHaveLength(MAX_TOASTS)
    expect(toasts[0].message).toBe('Toast 3')
  })

  it('ignores dismissing a toast that is already gone', () => {
    const id = toast.show('Once')
    dismiss(id)

    expect(() => dismiss(id)).not.toThrow()
    expect(toasts).toHaveLength(0)
  })
})
