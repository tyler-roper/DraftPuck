importScripts('https://www.gstatic.com/firebasejs/9.0.2/firebase-app-compat.js')
importScripts('https://www.gstatic.com/firebasejs/9.0.2/firebase-messaging-compat.js')
importScripts('/env-sw.js')

const firebaseConfig = {
  apiKey: self.env.VITE_FIREBASE_API_KEY,
  authDomain: self.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: self.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: self.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: self.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: self.env.VITE_FIREBASE_APP_ID
}

firebase.initializeApp(firebaseConfig)
const messaging = firebase.messaging()
