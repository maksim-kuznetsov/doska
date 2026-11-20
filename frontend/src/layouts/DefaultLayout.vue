<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { RouterView } from 'vue-router'

import AppMessage from '../components/AppMessage.vue'
import PwaInstallPrompt from '../components/PwaInstallPrompt.vue'
import PwaUpdatePrompt from '../components/PwaUpdatePrompt.vue'
import SiteFooter from '../components/SiteFooter.vue'
import SiteHeader from '../components/SiteHeader.vue'
import { usePwaStore } from '../stores/pwa'

const pwa = usePwaStore()
const { isOnline } = storeToRefs(pwa)
</script>

<template>
  <div class="app-shell">
    <SiteHeader />
    <PwaInstallPrompt />
    <PwaUpdatePrompt />
    <div v-if="!isOnline" class="app-shell__notice">
      <AppMessage variant="warning">Нет подключения к интернету</AppMessage>
    </div>
    <main class="site-main">
      <RouterView />
    </main>
    <SiteFooter />
  </div>
</template>