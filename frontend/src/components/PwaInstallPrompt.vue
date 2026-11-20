<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { computed } from 'vue'

import { usePwaStore } from '../stores/pwa'
import AppButton from './AppButton.vue'
import AppMessage from './AppMessage.vue'

const pwa = usePwaStore()
const { installAvailable, iosInstallHintAvailable } = storeToRefs(pwa)

const visible = computed(
  () => installAvailable.value || iosInstallHintAvailable.value,
)

async function install(): Promise<void> {
  await pwa.promptInstall()
}

function dismiss(): void {
  pwa.dismissInstall()
}
</script>

<template>
  <div v-if="visible" class="app-shell__notice">
    <AppMessage variant="info">
      <template v-if="installAvailable">
        <span class="pwa-prompt__text">
          Установите Doska на домашний экран: приложение открывается без адресной строки и
          работает при слабой связи.
        </span>
        <span class="pwa-prompt__actions">
          <AppButton size="compact" variant="primary" @click="install">
            Установить приложение
          </AppButton>
          <AppButton size="compact" variant="ghost" @click="dismiss">Не сейчас</AppButton>
        </span>
      </template>
      <template v-else>
        <span class="pwa-prompt__text">
          В Safari откройте «Поделиться» → «На экран „Домой“», чтобы установить приложение.
        </span>
        <span class="pwa-prompt__actions">
          <AppButton size="compact" variant="ghost" @click="dismiss">Понятно</AppButton>
        </span>
      </template>
    </AppMessage>
  </div>
</template>