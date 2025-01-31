<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useLobbyStore } from '@/stores/lobby'
import { ref } from 'vue'
import UserService from '@/services/UserService'

const lobbyStore = useLobbyStore()
const { setCurrentUserId } = lobbyStore
const userId = ref(localStorage.getItem('userId'))
;(async function onCreated() {
  let isValidUser = false
  if (userId.value) {
    try {
      await UserService.getUserById(userId.value)
      isValidUser = true
      console.log(`User validated. (${userId.value})`)
    } catch {
      console.log(`User invalid. (${userId.value})`)
    }
  }

  if (!isValidUser) {
    console.log(`Creating new user...`)
    const user = await UserService.createUser()
    localStorage.setItem('userId', user.id)
    userId.value = user.id
    console.log(`User created. (${userId.value})`)
  }

  setCurrentUserId(userId.value!)
})()
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
