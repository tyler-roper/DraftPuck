import { describe, expect, it } from 'vitest'
import LobbyEventType from '@/enums/lobbyEventType'
import { bannerStyle, isCallout, rainDrops } from '@/helpers/calloutHelpers'

describe('callouts', () => {
  it('are the four callout event types, each with an icon and colour', () => {
    const callouts = [LobbyEventType.OnFire, LobbyEventType.DrinkingStreak, LobbyEventType.RivalryBrewing, LobbyEventType.IceCold]

    for (const type of callouts) {
      expect(isCallout(type)).toBe(true)
      expect(bannerStyle(type)?.icon).toBeTruthy()
    }
    expect(bannerStyle(LobbyEventType.OnFire)?.icon).toBe('fire-flame-curved')
  })

  it("don't include ordinary feed events", () => {
    expect(isCallout(LobbyEventType.DrinkAwarded)).toBe(false)
    expect(bannerStyle(LobbyEventType.NewPick)).toBeUndefined()
  })

  it('mirror the server: callouts come after BotPicksMade, in order', () => {
    expect([LobbyEventType.OnFire, LobbyEventType.DrinkingStreak, LobbyEventType.RivalryBrewing, LobbyEventType.IceCold]).toEqual([18, 19, 20, 21])
  })

  it('drinks share the banner (with beer rain) without being callouts', () => {
    expect(bannerStyle(LobbyEventType.DrinkAwarded)?.emoji).toBe('🍺')
    expect(bannerStyle(LobbyEventType.DrinkAssigned)?.emoji).toBe('🍺')
    expect(isCallout(LobbyEventType.DrinkAwarded)).toBe(false)
  })

  it('each have an emoji to rain down', () => {
    expect(bannerStyle(LobbyEventType.OnFire)?.emoji).toBe('🔥')
    expect(bannerStyle(LobbyEventType.IceCold)?.emoji).toBe('❄️')
  })
})

describe('rainDrops', () => {
  it('spreads drops across the screen, all landing within the first few seconds', () => {
    const drops = rainDrops(20)

    expect(drops).toHaveLength(20)
    for (const d of drops) {
      expect(d.left).toBeGreaterThanOrEqual(0)
      expect(d.left).toBeLessThanOrEqual(98)
      expect(d.delayMs + d.durationMs).toBeLessThanOrEqual(4400)
    }
    // One in each twentieth of the width: no clumping on one side.
    expect(new Set(drops.map((d) => Math.floor(d.left / 5))).size).toBe(20)
  })

  it('is repeatable with a given random source', () => {
    const half = () => 0.5

    expect(rainDrops(1, half)[0]).toEqual({ left: 50, size: 32, delayMs: 700, durationMs: 2400, drift: 0, spin: 0 })
  })
})
