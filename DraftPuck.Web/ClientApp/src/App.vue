<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import { useSystemStore } from '@/stores/system'
import { useFirebaseStore } from '@/stores/firebase'
import { useGameSummariesStore } from './stores/gameSummaries'
import VToaster from '@/components/VToaster.vue'
import { useFriendsSync } from '@/composables/useFriendsSync'

const systemStore = useSystemStore()
const { loadGameSummaries: initializeGameSummaries } = useGameSummariesStore()
const { appIsTestMode, currentSystemTime } = storeToRefs(systemStore)
const { initAppSettings, updateSystemTime } = systemStore
const { initialize: initializeUser } = useUserStore()
const { initialize: initializeFirebase } = useFirebaseStore()
useFriendsSync()

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
    <!-- iOS colours the status bar of a home-screen app from a real element fixed at the very top (pseudo-elements
         don't count). Without one it leaves the bar transparent and blurs the page beneath it (iOS 26+). -->
    <div class="status-bar-colour" aria-hidden="true"></div>
    <RouterView />
    <VToaster />
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

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Only in the installed app: in a browser tab Safari draws its own bar. The app is always dark, so one colour (the
// headers' stone-900) suits every page; iOS samples it once, so it couldn't follow a light/dark switch anyway.
.status-bar-colour {
  display: none;
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 1px;
  background-color: map-get($custom-colors, 'stone-900');
  z-index: 2000;
  pointer-events: none;
}

@media (display-mode: standalone) {
  .status-bar-colour {
    display: block;
  }
}
</style>
