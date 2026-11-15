import { useRouter } from 'vue-router'

/**
 * Whether this page was reached by navigating within the app. Vue Router records the previous route in
 * `history.state.back`; it's null on the first page of a visit.
 */
export function hasInAppHistory(state: unknown = window.history.state): boolean {
  return typeof state === 'object' && state !== null && typeof (state as { back?: unknown }).back === 'string'
}

/**
 * A back action: back through the app's history, or to `fallback()` when the page was opened directly (a link, a
 * push notification, the first screen of the installed app). There, `router.back()` does nothing or leaves the app,
 * and an installed app has no browser controls to get anywhere else.
 */
export function useBackNavigation(fallback: () => string) {
  const router = useRouter()

  return function goBack() {
    if (hasInAppHistory()) router.back()
    else router.replace(fallback())
  }
}
