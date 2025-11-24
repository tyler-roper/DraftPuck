import { defineStore } from 'pinia'
import { ref } from 'vue'
import { initializeApp } from 'firebase/app'
import { getMessaging, getToken, onMessage } from 'firebase/messaging'
import { useUserStore } from '@/stores/user'
import { env } from '@/env'
import { useToast } from 'vue-toastification'

export const useFirebaseStore = defineStore('firebase', () => {
  const isInitialized = ref(false)
  const notificationsSupported = ref('Notification' in window)
  const permissionGranted = ref(Notification.permission === 'granted')
  const toast = useToast()

  async function initialize() {
    if (isInitialized.value) return
    isInitialized.value = true

    if (!notificationsSupported.value || !permissionGranted.value) {
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
        const type = payload.data?.type;
        const title = payload.notification?.title;
        const body = payload.notification?.body;

        if (type === "Achievement") {
          toast.success(`Achievement earned! ${title} — ${body}`);
        }
      });

      const token = await getToken(messaging, {
        vapidKey: env.VITE_FIREBASE_VAPID_KEY,
      })

      await userStore.updateUser({ fcmRegistrationToken: token })
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
    permissionGranted,
    initialize,
  }
})
