import { ref } from 'vue'
import { defineStore } from 'pinia'
import SystemService from '@/services/SystemService'
import { differenceInMilliseconds, addMilliseconds } from 'date-fns'

export const useSystemStore = defineStore('system', () => {
  //#region state
  const appIsTestMode = ref(false)
  const appStartupTime = ref(new Date())
  const appTestModeStartTime = ref(new Date())
  const currentSystemTime = ref(new Date())
  const gitSha = ref('')
  //#endregion

  //#region mutations
  const setAppSettings = (appSettings: { appIsTestMode: boolean; appStartupTime: Date; appTestModeStartTime: Date; gitSha: string }) => {
    appIsTestMode.value = appSettings.appIsTestMode
    appStartupTime.value = appSettings.appStartupTime
    appTestModeStartTime.value = appSettings.appTestModeStartTime
    gitSha.value = appSettings.gitSha
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

  async function initAppSettings() {
    const result = await SystemService.getSettings()
    setAppSettings({
      appIsTestMode: result.isTestMode,
      appStartupTime: result.startupTimeUtc,
      appTestModeStartTime: result.testModeStartDateTimeUtc,
      gitSha: result.gitSha
    })
  }
  //#endregion

  //#region getters
  //#endregion

  return {
    setAppSettings,
    initAppSettings,
    updateSystemTime,
    currentSystemTime,
    appIsTestMode,
    gitSha
  }
})
