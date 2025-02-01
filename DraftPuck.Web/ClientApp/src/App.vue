<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useLobbyStore } from '@/stores/lobby'
import PullToRefresh from 'pulltorefreshjs'
import SystemService from '@/services/SystemService'

const { setCurrentUserId } = useLobbyStore()
const { initUser } = useUserStore()

;(async function onCreated() {
  window.addEventListener('unhandledrejection', function(event) {
    try {
      SystemService.reportError(event.promise, event.reason)
    } catch {}
 });

  enableRefreshOniOS()
  const user = await initUser()
  if (user) setCurrentUserId(user.id)
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
    <RouterView />
  </div>
</template>

<style>
#app {
  height: 100%;
}
</style>
