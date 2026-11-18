import { createPinia } from 'pinia'
import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
import './assets/css/tokens.css'
import './assets/css/base.css'
import './assets/css/components.css'
import './assets/css/layout.css'

createApp(App).use(createPinia()).use(router).mount('#app')