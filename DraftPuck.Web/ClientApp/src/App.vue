<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useLobbyStore } from '@/stores/lobby'
import { ref } from 'vue'
import UserService from '@/services/UserService'
import { initializeApp } from 'firebase/app'
import { getToken, onMessage, getMessaging } from 'firebase/messaging'

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
  const token = await initializeFirebase();
  await UserService.updateFcmRegistrationToken(userId.value!, { token });
})()

async function initializeFirebase() {
  const firebaseConfig = {
    apiKey: 'AIzaSyBGw_anxN2MDnfPSTyvqmfmYAwKTdLBOAY',
    authDomain: 'draftpuck.firebaseapp.com',
    projectId: 'draftpuck',
    storageBucket: 'draftpuck.firebasestorage.app',
    messagingSenderId: '34141903027',
    appId: '1:34141903027:web:7d676e25fe00fcb582b8c6'
  }

  const app = initializeApp(firebaseConfig)
  const messaging = getMessaging(app)
  onMessage(messaging, (payload) => {
    console.log(`Message received: ${payload}`)
  })
  return await getToken(messaging, { vapidKey: 'BOngebl5Rmrgo0k0YMjstWPapJ-Zl0Izbbsyl0l0lI7L9cmHiDdcLUEj3moGuibR_YxTfGYKC134nSB42ZxxTaA' })
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
