import { ref } from 'vue'
import { defineStore } from 'pinia'
import SystemService from '@/services/SystemService'
import { differenceInMilliseconds, addMilliseconds } from 'date-fns'

export type LogoVariant = 'standard' | 'wide'

const DEFAULT_LOGOS: Record<LogoVariant, string> = {
  standard: '/img/logo.png',
  wide: '/img/logo-wide.png'
}

export const useSystemStore = defineStore('system', () => {
  //#region state
  const appIsTestMode = ref(false)
  const appStartupTime = ref(new Date())
  const appTestModeStartTime = ref(new Date())
  const currentSystemTime = ref(new Date())
  const gitSha = ref('')
  const eventLogo = ref<SystemSettingsResponse['eventLogo']>(null)
  //#endregion

  //#region mutations
  const setAppSettings = (appSettings: {
    appIsTestMode: boolean
    appStartupTime: Date
    appTestModeStartTime: Date
    gitSha: string
    eventLogo?: SystemSettingsResponse['eventLogo']
  }) => {
    appIsTestMode.value = appSettings.appIsTestMode
    appStartupTime.value = appSettings.appStartupTime
    appTestModeStartTime.value = appSettings.appTestModeStartTime
    gitSha.value = appSettings.gitSha
    eventLogo.value = appSettings.eventLogo ?? null
  }

  //#endregion

  //#region actions
  function updateSystemTime() {
    if (!appIsTestMode.value) {
      currentSystemTime.value = new Date()
      return
    }

    const millisecondsSinceStartup = differenceInMilliseconds(new Date(), appStartupTime.value)
    currentSystemTime.value = addMilliseconds(appTestModeStartTime.value, millisecondsSinceStartup)
  }

  /** Loaded once, when the site starts (App.vue), including the current event's logo. */
  async function initAppSettings() {
    const result = await SystemService.getSettings()
    setAppSettings({
      appIsTestMode: result.isTestMode,
      appStartupTime: result.startupTimeUtc,
      appTestModeStartTime: result.testModeStartDateTimeUtc,
      gitSha: result.gitSha,
      eventLogo: result.eventLogo
    })
  }
  //#endregion

  //#region getters
  /** The logo to show: the current event's if one is on, otherwise the usual one. */
  function logoUrl(variant: LogoVariant) {
    const event = eventLogo.value
    if (!event) return DEFAULT_LOGOS[variant]
    return variant === 'wide' ? event.wideLogoUrl : event.logoUrl
  }
  //#endregion

  return {
    setAppSettings,
    initAppSettings,
    updateSystemTime,
    currentSystemTime,
    appIsTestMode,
    gitSha,
    eventLogo,
    logoUrl
  }
})
