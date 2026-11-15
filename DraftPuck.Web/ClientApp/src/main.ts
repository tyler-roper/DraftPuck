import '@/assets/scss/site.scss'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import Popper from 'vue3-popper'
import '@/assets/scss/popper.css'

import 'bootstrap'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.component('VPopper', Popper)

app.mount('#app')
