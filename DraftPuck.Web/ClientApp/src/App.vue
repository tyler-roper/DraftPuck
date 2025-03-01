<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useLobbyStore } from '@/stores/lobby'
import PullToRefresh from 'pulltorefreshjs'
import { storeToRefs } from 'pinia'
import { format } from 'date-fns'
import { ref } from 'vue'
const lobbyStore = useLobbyStore()

const { appIsTestMode, currentSystemTime } = storeToRefs(lobbyStore)
const { setCurrentUserId, initAppSettings, updateSystemTime } = lobbyStore
const { initUser } = useUserStore()

const hasUser = ref(false)

;(async function onCreated() {
  const user = await initUser()
  if (user) {
    hasUser.value = true
    setCurrentUserId(user.id)
  }

  await initAppSettings()
  window.setInterval(updateSystemTime, 1000)
  enableRefreshOniOS()
})()

function enableRefreshOniOS() {
  //@ts-ignore
  const isInWebAppiOS = window.navigator.standalone === true
  if (isInWebAppiOS) {
    PullToRefresh.init({
      mainElement: 'body',
      onRefresh() {
        window.location.reload()
      }
    })
  }
}
</script>

<template>
  <div id="app">
    <div class="system-timer" v-if="appIsTestMode">
      {{ format(currentSystemTime, 'Pp') }}
    </div>
    <template v-if="hasUser">
      <RouterView />
    </template>
  </div>
</template>

<style>
#app {
  height: 100%;
}

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
