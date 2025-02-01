<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useLobbyStore } from '@/stores/lobby'
import PullToRefresh from 'pulltorefreshjs'

const { setCurrentUserId } = useLobbyStore()
const { initUser } = useUserStore()

;(async function onCreated() {
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
