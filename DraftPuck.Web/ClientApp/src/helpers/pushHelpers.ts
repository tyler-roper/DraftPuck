export interface ForegroundToast {
  title: string
  body: string
  // In-app path to open when the toast is clicked, if the push has a link.
  path?: string
}

/**
 * Decides what to show for a push that arrives while the app is open. Firebase hands foreground pushes to the page
 * instead of the service worker, so without this they'd be dropped and no notification shown.
 * Returns null when nothing should be shown: lobby events for the lobby being viewed already appear live on the page.
 */
export function getForegroundToast(data: Record<string, string> | undefined, currentPath: string): ForegroundToast | null {
  const title = data?.title
  if (!title) return null

  const path = toAppPath(data?.link)
  if (data?.type === 'LobbyEvent' && path && samePath(path, currentPath)) return null

  return { title, body: data?.body ?? '', path }
}

// Links are absolute (https://draftpuck.com/lobby/ABCD); the router needs the path.
function toAppPath(link: string | undefined): string | undefined {
  if (!link) return undefined
  try {
    return new URL(link, window.location.origin).pathname
  } catch {
    return undefined
  }
}

function samePath(a: string, b: string) {
  const normalize = (p: string) => p.replace(/\/+$/, '').toLowerCase()
  return normalize(a) === normalize(b)
}

/**
 * Whether DraftPuck is running as an installed app (from the home screen on iOS or Android, or installed from Chrome
 * or Edge on a computer) rather than in a browser tab. Sent with a device's registration so it can be described.
 */
export function isRunningAsInstalledApp(win: Pick<Window, 'matchMedia' | 'navigator'> = window): boolean {
  if (win.matchMedia?.('(display-mode: standalone)').matches) return true
  // Safari on iOS predates display-mode and uses its own flag.
  return (win.navigator as Navigator & { standalone?: boolean }).standalone === true
}
