import LobbyEventType from '@/enums/lobbyEventType'

export interface CalloutStyle {
  /** A Flaticon solid-rounded icon name. */
  icon: string
  color: string
  /** How the banner's icon moves. */
  motion: 'flicker' | 'wobble' | 'clash' | 'shiver'
  /** What rains down the screen behind the banner. */
  emoji: string
}

// Callouts have the icons and colours of the matching end-of-night awards (recapHelpers). Drinks share the banner too.
const bannerStyles: Partial<Record<LobbyEventType, CalloutStyle>> = {
  [LobbyEventType.OnFire]: { icon: 'fire-flame-curved', color: '#ff6a00', motion: 'flicker', emoji: '🔥' },
  [LobbyEventType.DrinkingStreak]: { icon: 'glass-cheers', color: '#dc3545', motion: 'wobble', emoji: '🥴' },
  [LobbyEventType.RivalryBrewing]: { icon: 'sword', color: '#f97316', motion: 'clash', emoji: '⚔️' },
  [LobbyEventType.IceCold]: { icon: 'snowflake', color: '#00ccff', motion: 'shiver', emoji: '❄️' },
  [LobbyEventType.DrinkAwarded]: { icon: 'beer', color: '#ffb100', motion: 'wobble', emoji: '🍺' },
  [LobbyEventType.DrinkAssigned]: { icon: 'beer', color: '#ffb100', motion: 'wobble', emoji: '🍺' }
}

const callouts = new Set([LobbyEventType.OnFire, LobbyEventType.DrinkingStreak, LobbyEventType.RivalryBrewing, LobbyEventType.IceCold])

/** How the event looks in a banner (callouts, and drinks earned or handed out), or undefined if it gets no banner. */
export function bannerStyle(type: LobbyEventType): CalloutStyle | undefined {
  return bannerStyles[type]
}

/** Callouts (on fire, a rivalry...) are notable moments: a banner, and a highlighted entry in the feed. */
export function isCallout(type: LobbyEventType): boolean {
  return callouts.has(type)
}

export interface RainDrop {
  /** Across the screen, in %. */
  left: number
  /** In px. */
  size: number
  /** When it starts falling, and how long the fall takes, in ms. */
  delayMs: number
  durationMs: number
  /** Sideways drift over the fall, in px, and the spin, in degrees. */
  drift: number
  spin: number
}

/**
 * The drops for a callout's emoji rain: spread across the screen, a mix of sizes and speeds, all falling within the
 * banner's first few seconds. `random` is injectable for tests.
 */
export function rainDrops(count = 28, random: () => number = Math.random): RainDrop[] {
  const between = (min: number, max: number) => min + random() * (max - min)
  return Array.from({ length: count }, (_, i) => ({
    // Spread evenly, then jittered, so drops don't clump.
    left: Math.min(98, Math.max(0, ((i + random()) / count) * 100)),
    size: Math.round(between(20, 44)),
    delayMs: Math.round(between(0, 1400)),
    durationMs: Math.round(between(1800, 3000)),
    drift: Math.round(between(-40, 40)),
    spin: Math.round(between(-180, 180))
  }))
}
