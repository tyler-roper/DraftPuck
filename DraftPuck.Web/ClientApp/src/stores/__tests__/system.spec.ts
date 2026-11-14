import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSystemStore } from '@/stores/system'
import SystemService from '@/services/SystemService'

vi.mock('@/services/SystemService', () => ({ default: { getSettings: vi.fn() } }))

const settings = (eventLogo: SystemSettingsResponse['eventLogo']) =>
  ({ isTestMode: false, startupTimeUtc: new Date(), testModeStartDateTimeUtc: new Date(), gitSha: 'abc', eventLogo }) as SystemSettingsResponse

describe('system store logos', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('uses the usual logos when no event is on', async () => {
    vi.mocked(SystemService.getSettings).mockResolvedValue(settings(null))
    const store = useSystemStore()

    await store.initAppSettings()

    expect(store.logoUrl('standard')).toBe('/img/logo.png')
    expect(store.logoUrl('wide')).toBe('/img/logo-wide.png')
  })

  it("uses the event's logos while one is on", async () => {
    vi.mocked(SystemService.getSettings).mockResolvedValue(
      settings({ name: '4 Nations', logoUrl: '/img/logo-4nations.png', wideLogoUrl: '/img/logo-wide-4nations.png' })
    )
    const store = useSystemStore()

    await store.initAppSettings()

    expect(store.logoUrl('standard')).toBe('/img/logo-4nations.png')
    expect(store.logoUrl('wide')).toBe('/img/logo-wide-4nations.png')
  })

  it('fetches the settings, and so the logo, once per visit', async () => {
    vi.mocked(SystemService.getSettings).mockClear()
    vi.mocked(SystemService.getSettings).mockResolvedValue(settings(null))
    const store = useSystemStore()

    await store.initAppSettings()
    store.logoUrl('wide')
    store.logoUrl('standard')

    expect(SystemService.getSettings).toHaveBeenCalledOnce()
  })
})
