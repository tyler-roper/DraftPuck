import '@/assets/scss/site.scss'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import Toast, { POSITION } from 'vue-toastification'
import 'vue-toastification/dist/index.css'

import Popper from 'vue3-popper'
import '@/assets/scss/popper.css'

import 'bootstrap'

import App from './App.vue'
import router from './router'

import SystemService from '@/services/SystemService'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(Toast, { position: POSITION.BOTTOM_CENTER })
app.component('VPopper', Popper)

if (import.meta.env.PROD) {
  window.onerror = function (msg, url, line, col, error) {
    window.alert(`${msg} | ${url} | ${line} | ${col} | ${error}`)
  }

  app.config.errorHandler = function (err, _vm, info) {
    window.alert(err + ' | ' + info)
  }
}

app.mount('#app')
