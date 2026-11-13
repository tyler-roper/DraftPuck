/* eslint-env serviceworker */
/* global firebase */ // loaded by importScripts below
importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-app-compat.js')
importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-messaging-compat.js')
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

// Messages are data-only, so the SDK never auto-displays them; this is the only place a notification is shown.
// The SDK awaits this handler inside the push event, so returning the promise keeps the event alive until it's shown.
messaging.onBackgroundMessage(function(payload) {
  const data = payload.data ?? {};

  return self.registration.showNotification(data.title || 'DraftPuck', {
    body: data.body,
    icon: data.icon || '/img/icons/icon-192.png',
    badge: data.badge || '/img/icons/badge.png',
    vibrate: [200, 100, 200],
    data
  });
});

self.addEventListener('notificationclick', function(event) {
  event.notification.close();

  const link = event.notification.data?.link;
  if (link) {
    event.waitUntil(clients.openWindow(link));
  }
});
