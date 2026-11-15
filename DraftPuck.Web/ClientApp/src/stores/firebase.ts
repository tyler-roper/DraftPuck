import { defineStore } from 'pinia'
import { ref } from 'vue'
import { initializeApp } from 'firebase/app'
import { getMessaging, getToken, onMessage } from 'firebase/messaging'
import { useUserStore } from '@/stores/user'
import { env } from '@/env'
import { useToast } from '@/composables/useToast'
import { escapeHtml } from '@/helpers/htmlHelpers'
import { getForegroundToast } from '@/helpers/pushHelpers'
import router from '@/router'

export const useFirebaseStore = defineStore('firebase', () => {
  const isInitialized = ref(false)
  const notificationsSupported = ref('Notification' in window)
  const toast = useToast()

  const isPermissionGranted = () => Notification.permission === 'granted'

  async function initialize() {
    if (isInitialized.value) return

    if (!notificationsSupported.value || !isPermissionGranted()) {
      await clearUserFcmToken()
      return
    }

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

      const token = await getToken(messaging, {
        vapidKey: env.VITE_FIREBASE_VAPID_KEY
      })

      await userStore.updateUser({ fcmRegistrationToken: token })

      isInitialized.value = true
    } catch (err) {
      console.error('Firebase init failed', err)
    }
  }

  async function clearUserFcmToken() {
    const userStore = useUserStore()
    await userStore.updateUser({ fcmRegistrationToken: '' })
  }

  return {
    isInitialized,
    notificationsSupported,
    initialize
  }
})
