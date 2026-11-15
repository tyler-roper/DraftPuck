import { reactive, readonly } from 'vue'

export type ToastType = 'default' | 'info' | 'success' | 'error'

export interface ToastOptions {
  /** Milliseconds before the toast closes itself, or false to keep it until it's closed. Defaults to 5 seconds. */
  timeout?: number | false
  /** Icon classes (e.g. `fi fi-sr-bell`), replacing the type's own icon. */
  icon?: string
  /** Runs when the toast is clicked, before it closes. */
  onClick?: () => void
}

export interface HtmlToastOptions extends ToastOptions {
  type?: ToastType
  /** Shown above the message, as HTML. */
  title?: string
}

export interface Toast {
  id: number
  type: ToastType
  /** Plain text, or HTML when `isHtml` is set. */
  message: string
  title?: string
  isHtml: boolean
  /** Milliseconds it stays open, or false. Drives the countdown bar. */
  timeout: number | false
  icon?: string
  onClick?: () => void
}

export const DEFAULT_TIMEOUT_MS = 5000
/** More than this at once and the oldest ones close. */
export const MAX_TOASTS = 5

interface Countdown {
  handle?: ReturnType<typeof setTimeout>
  remainingMs: number
  startedAt: number
}

// Module state: one stack of toasts for the whole page, shown by VToaster.
const toasts = reactive<Toast[]>([])
const countdowns = new Map<number, Countdown>()
let nextId = 1

function add(type: ToastType, message: string, isHtml: boolean, options: HtmlToastOptions = {}): number {
  const id = nextId++
  const timeout = options.timeout ?? DEFAULT_TIMEOUT_MS
  toasts.push({ id, type, message, isHtml, timeout, title: options.title, icon: options.icon, onClick: options.onClick })

  if (timeout !== false) {
    countdowns.set(id, { remainingMs: timeout, startedAt: 0 })
    resume(id)
  }

  while (toasts.length > MAX_TOASTS) dismiss(toasts[0].id)
  return id
}

function dismiss(id: number) {
  clearTimeout(countdowns.get(id)?.handle)
  countdowns.delete(id)

  const index = toasts.findIndex((t) => t.id === id)
  if (index >= 0) toasts.splice(index, 1)
}

/** Stops the toast's countdown, e.g. while it's hovered. */
function pause(id: number) {
  const countdown = countdowns.get(id)
  if (countdown?.handle === undefined) return

  clearTimeout(countdown.handle)
  countdown.handle = undefined
  countdown.remainingMs -= Date.now() - countdown.startedAt
}

function resume(id: number) {
  const countdown = countdowns.get(id)
  if (!countdown || countdown.handle !== undefined) return

  countdown.startedAt = Date.now()
  countdown.handle = setTimeout(() => dismiss(id), Math.max(0, countdown.remainingMs))
}

export const toast = {
  /** A neutral toast. Messages are plain text. */
  show: (message: string, options?: ToastOptions) => add('default', message, false, options),
  info: (message: string, options?: ToastOptions) => add('info', message, false, options),
  success: (message: string, options?: ToastOptions) => add('success', message, false, options),
  error: (message: string, options?: ToastOptions) => add('error', message, false, options),
  /** A toast whose title and message are HTML: escape anything user-supplied in them first. */
  html: (message: string, options: HtmlToastOptions = {}) => add(options.type ?? 'default', message, true, options),
  dismiss
}

export function useToast() {
  return toast
}

/** The open toasts and their controls, for the component that shows them. */
export function useToastStack() {
  return { toasts: readonly(toasts), dismiss, pause, resume }
}
