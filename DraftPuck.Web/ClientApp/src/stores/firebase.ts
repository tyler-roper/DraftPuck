import { defineStore } from 'pinia'
import { initializeApp } from 'firebase/app'
import { getToken, getMessaging, onMessage } from 'firebase/messaging'
import UserService from '@/services/UserService'
import { ref, computed } from 'vue'
import { useToast } from 'vue-toastification'

const vapidKey = 'BKj46v7TX9RsVT86YwqQiF2Bg3K6zf8D4ofsJOdBqd9DZJx-aXkEERAlMEaZZxih8nPe4HVo8Nescw7kmHV3OUY'
const firebaseConfig = {
  apiKey: 'AIzaSyBGw_anxN2MDnfPSTyvqmfmYAwKTdLBOAY',
  authDomain: 'draftpuck.firebaseapp.com',
  projectId: 'draftpuck',
  storageBucket: 'draftpuck.firebasestorage.app',
  messagingSenderId: '34141903027',
  appId: '1:34141903027:web:7d676e25fe00fcb582b8c6'
}

export const useFirebaseStore = defineStore('firebase', () => {
  //#region state
  const fcmToken = ref<string>()
  const currentUserId = ref<string>()
  const messaging = ref()
  const toast = useToast()
  const isNotificationSupported = ref('Notification' in window)
  const isNotificationPermissionGranted = ref(Notification.permission === 'granted')
  //#endregion

  //#region mutations
  const setToken = (token?: string) => (fcmToken.value = token)
  const setCurrentUserId = (_currentUserId: string) => (currentUserId.value = _currentUserId)
  //#endregion

  //#region actions
  async function getFcmToken() {
    const app = initializeApp(firebaseConfig)
    messaging.value = getMessaging(app)
    const token = await getToken(messaging.value, { vapidKey })
    await updateUserFcmToken(token)
    setToken(token)

    onMessage(messaging.value, async ({ notification, ..._ }) => {
      if (!notification?.title || !notification?.body) return
      toast(`${notification.title}! ${notification.body}`)
    })
  }

  async function updateUserFcmToken(token: string) {
    await UserService.updateFcmRegistrationToken(currentUserId.value!, { token })
  }

  async function clearUserFcmToken() {
    await UserService.updateFcmRegistrationToken(currentUserId.value!, { token: undefined })
    setToken(undefined)
  }

  async function requestNotificationPermission() {
    await Notification.requestPermission()
    isNotificationPermissionGranted.value = Notification.permission === 'granted'
    if (isNotificationPermissionGranted.value) getFcmToken()
    else clearUserFcmToken()
  }

  //#endregion

  //#region getters

  //#endregion

  return {
    fcmToken,
    currentUserId,
    messaging,
    setToken,
    setCurrentUserId,
    getFcmToken,
    updateUserFcmToken,
    clearUserFcmToken,
    isNotificationPermissionGranted,
    requestNotificationPermission,
    isNotificationSupported
  }
})
