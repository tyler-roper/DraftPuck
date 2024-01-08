import Vue from 'vue'
import App from './App.vue'
import { BootstrapVue } from 'bootstrap-vue'
import router from '@/plugins/router'
import store from '@/store'
import VueToast from 'vue-toast-notification'
import 'vue-toast-notification/dist/theme-sugar.css'
import VueCurrencyInput from 'vue-currency-input'
import VueLuxon from "vue-luxon";
import VueScrollTo from 'vue-scrollto';
import '@/assets/scss/site.scss'

Vue.config.productionTip = false

const currencyOptions = {
    globalOptions: { currency: 'USD' }
}

Vue.use(VueScrollTo);
Vue.use(VueLuxon);
Vue.use(VueCurrencyInput, currencyOptions)
Vue.use(BootstrapVue);
Vue.use(VueToast, {
    position: "bottom",
    type: "default"
});

Vue.directive('click-outside', {
    bind: function (el, binding, vnode) {
        el.clickOutsideEvent = function (event) {
            if (!el.contains(event.target)) {
                vnode.context[binding.expression](event);
            }
        };
        document.body.addEventListener('mousedown', el.clickOutsideEvent)
    },
    unbind: function (el) {
        document.body.removeEventListener('mousedown', el.clickOutsideEvent)
    }
});

new Vue({
    router,
    store,
    render: (h: Function) => h(App)
}).$mount('#app');