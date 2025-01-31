import { defineStore } from 'pinia'
import { initializeApp } from 'firebase/app'
import { getToken, getMessaging, onMessage } from 'firebase/messaging'
import UserService from '@/services/UserService'
import { ref } from 'vue'
import { useToast } from 'vue-toastification'

const vapidKey = import.meta.env.VITE_FIREBASE_VAPID_KEY
const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID
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
