<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useFirebaseStore } from '@/stores/firebase'
import { useLobbyStore } from '@/stores/lobby'
import { ref } from 'vue'
import UserService from '@/services/UserService'
import { onMessage } from 'firebase/messaging'
import { storeToRefs } from 'pinia'
import { useToast } from 'vue-toastification'

const lobbyStore = useLobbyStore()
const firebaseStore = useFirebaseStore()
const { setCurrentUserId: setCurrentUserIdFirebase, getFcmToken } = firebaseStore
const { isNotificationSupported, isNotificationPermissionGranted } = storeToRefs(firebaseStore)
const { setCurrentUserId } = lobbyStore
const userId = ref(localStorage.getItem('userId'))
const { messaging } = storeToRefs(firebaseStore)
const toast = useToast()

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

  setCurrentUserIdFirebase(userId.value!)
  setCurrentUserId(userId.value!)

  if (isNotificationSupported.value && isNotificationPermissionGranted.value) {
    await getFcmToken()
  }

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
