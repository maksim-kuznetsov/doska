<script setup lang="ts">
import { storeToRefs } from 'pinia'

import { usePwaStore } from '../stores/pwa'
import AppButton from './AppButton.vue'
import AppMessage from './AppMessage.vue'

const pwa = usePwaStore()
const { updateAvailable } = storeToRefs(pwa)

async function apply(): Promise<void> {
  await pwa.applyUpdate()
}

function later(): void {
  pwa.dismissUpdate()
}
</script>

<template>
  <div v-if="updateAvailable" class="app-shell__notice">
    <AppMessage variant="info">
      <span class="pwa-prompt__text">Доступна новая версия приложения.</span>
      <span class="pwa-prompt__actions">
        <AppButton size="compact" variant="primary" @click="apply">Обновить</AppButton>
        <AppButton size="compact" variant="ghost" @click="later">Позже</AppButton>
      </span>
    </AppMessage>
  </div>
</template>