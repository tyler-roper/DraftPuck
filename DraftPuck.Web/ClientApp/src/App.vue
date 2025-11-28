<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import { useSystemStore } from '@/stores/system'
import { useFirebaseStore } from '@/stores/firebase'
import { useGameSummariesStore } from './stores/gameSummaries'

const systemStore = useSystemStore()
const { loadGameSummaries: initializeGameSummaries } = useGameSummariesStore()
const { appIsTestMode, currentSystemTime } = storeToRefs(systemStore)
const { initAppSettings, updateSystemTime } = systemStore
const { initialize: initializeUser } = useUserStore()
const { initialize: initializeFirebase } = useFirebaseStore()

onMounted(async () => {
  initializeUser().then(initializeFirebase)
  initializeGameSummaries()
  await initAppSettings()

  updateSystemTime()
  window.setInterval(updateSystemTime, 1000)
})
</script>

<template>
  <div class="h-100">
    <div class="system-timer" v-if="appIsTestMode">
      {{ format(currentSystemTime, 'Pp') }}
    </div>
    <RouterView />
  </div>
</template>

<style scoped>
.system-timer {
  position: fixed;
  padding: 5px;
  background: red;
  color: white;
  top: 0;
  left: 0;
  z-index: 99;
}
</style>
