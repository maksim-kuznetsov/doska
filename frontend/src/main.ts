import { createPinia } from 'pinia'
import { createApp } from 'vue'

import App from './App.vue'
import OfflineApp from './pwa/OfflineApp.vue'
import { registerPwaServiceWorker } from './pwa/registerServiceWorker'
import router from './router'
import { usePwaStore } from './stores/pwa'
import './assets/css/tokens.css'
import './assets/css/base.css'
import './assets/css/components.css'
import './assets/css/layout.css'

if (navigator.onLine === false) {
  // Офлайн-старт обходит роутер и Pinia, чтобы не обращаться к API.
  createApp(OfflineApp).mount('#app')
} else {
  const pinia = createPinia()
  const app = createApp(App).use(pinia).use(router)
  const pwa = usePwaStore(pinia)

  pwa.listen()
  registerPwaServiceWorker(pwa)

  app.mount('#app')
}