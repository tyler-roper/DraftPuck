importScripts('https://www.gstatic.com/firebasejs/9.0.2/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.0.2/firebase-messaging-compat.js');

const firebaseConfig = {
  apiKey: 'AIzaSyBGw_anxN2MDnfPSTyvqmfmYAwKTdLBOAY',
  authDomain: 'draftpuck.firebaseapp.com',
  projectId: 'draftpuck',
  storageBucket: 'draftpuck.firebasestorage.app',
  messagingSenderId: '34141903027',
  appId: '1:34141903027:web:7d676e25fe00fcb582b8c6'
}

firebase.initializeApp(firebaseConfig)

const messaging = firebase.messaging();
messaging.onBackgroundMessage((payload) => {
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  // Customize notification here
  const notificationTitle = 'Background Message Title';
  const notificationOptions = {
    body: 'Background Message body.',
    icon: '/firebase-logo.png'
  };

  self.registration.showNotification(notificationTitle,
    notificationOptions);
});