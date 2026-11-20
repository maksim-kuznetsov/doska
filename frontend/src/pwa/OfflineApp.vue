<script setup lang="ts">
import { ref } from 'vue'

import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'

const retrying = ref(false)

function retry(): void {
  retrying.value = true
  window.location.reload()
}
</script>

<template>
  <main class="offline-app">
    <section class="offline-app__panel" aria-labelledby="offline-title">
      <h1 id="offline-title" class="offline-app__title">Нет подключения к интернету</h1>
      <AppMessage variant="info">
        Проверьте подключение к сети и повторите попытку. Данные всегда загружаются с сервера.
      </AppMessage>
      <AppButton variant="primary" block :disabled="retrying" @click="retry">
        Повторить
      </AppButton>
    </section>
  </main>
</template>