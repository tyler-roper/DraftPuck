import { defineStore } from 'pinia'
import { ref } from 'vue'
import { initializeApp } from 'firebase/app'
import { getMessaging, getToken, onMessage } from 'firebase/messaging'
import { useUserStore } from '@/stores/user'
import { env } from '@/env'
import { useToast } from '@/composables/useToast'
import { escapeHtml } from '@/helpers/htmlHelpers'
import { getForegroundToast, isRunningAsInstalledApp } from '@/helpers/pushHelpers'
import UserService from '@/services/UserService'
import router from '@/router'
import { useFriendsStore } from '@/stores/friends'

export const useFirebaseStore = defineStore('firebase', () => {
  const isInitialized = ref(false)
  // This device's Firebase token, once notifications are allowed.
  const currentToken = ref<string>()
  const notificationsSupported = ref('Notification' in window)
  const toast = useToast()

  const isPermissionGranted = () => Notification.permission === 'granted'

  async function initialize() {
    if (isInitialized.value) return

    // Without permission this device simply doesn't register. (It used to clear the account's one token, which
    // stopped the user's other devices getting pushes too.)
    if (!notificationsSupported.value || !isPermissionGranted()) return

    const firebaseConfig = {
      apiKey: env.VITE_FIREBASE_API_KEY,
      authDomain: env.VITE_FIREBASE_AUTH_DOMAIN,
      projectId: env.VITE_FIREBASE_PROJECT_ID,
      storageBucket: env.VITE_FIREBASE_STORAGE_BUCKET,
      messagingSenderId: env.VITE_FIREBASE_MESSAGING_SENDER_ID,
      appId: env.VITE_FIREBASE_APP_ID
    }

    try {
      const app = initializeApp(firebaseConfig)
      const messaging = getMessaging(app)
      const userStore = useUserStore()

      // Handle foreground messages
      onMessage(messaging, (payload) => {
        const type = payload.data?.type
        const title = payload.data?.title
        const body = payload.data?.body

        if (type === 'Achievement') {
          toast.html(escapeHtml(body ?? ''), {
            title: `<span class='text-primary'>${escapeHtml(title ?? '')}</span>`,
            icon: 'fi fi-sr-trophy'
          })
          userStore.refreshUser()
          return
        }

        // Friend requests and lobby invites change what's waiting on the user (the badge).
        if (type === 'Friend') useFriendsStore().load().catch(() => {})

        // Foreground pushes skip the service worker, so show other notifications (picks ready, drinks, chat) in-app.
        const notification = getForegroundToast(payload.data, router.currentRoute.value.path)
        if (!notification) return

        const { path } = notification
        // The toast renders HTML and chat pushes carry other members' text, so escape both.
        toast.html(escapeHtml(notification.body), {
          title: escapeHtml(notification.title),
          icon: 'fi fi-sr-bell',
          onClick: path ? () => router.push(path) : undefined
        })
      })

      currentToken.value = await getToken(messaging, {
        vapidKey: env.VITE_FIREBASE_VAPID_KEY
      })

      await registerCurrentDevice()

      isInitialized.value = true
    } catch (err) {
      console.error('Firebase init failed', err)
    }
  }

  /** Registers this device for whoever is signed in now: on start, and again after signing in or out. */
  async function registerCurrentDevice() {
    if (!currentToken.value) return
    try {
      await UserService.registerDevice(currentToken.value, isRunningAsInstalledApp())
    } catch (err) {
      console.error('Registering this device for push notifications failed', err)
    }
  }

  /** Stops this device getting the signed-in user's pushes, before they sign out. */
  async function forgetCurrentDevice() {
    if (!currentToken.value) return
    try {
      await UserService.forgetDevice(currentToken.value)
    } catch (err) {
      console.error('Forgetting this device failed', err)
    }
  }

  return {
    isInitialized,
    notificationsSupported,
    initialize,
    registerCurrentDevice,
    forgetCurrentDevice
  }
})
