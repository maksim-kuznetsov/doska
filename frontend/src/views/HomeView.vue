<script setup lang="ts">
import { storeToRefs } from 'pinia'

import { useAppStore } from '../stores/app'

const { appName } = storeToRefs(useAppStore())
</script>

<template>
  <section class="home">
    <h1 class="home__title">{{ appName }}</h1>
    <p class="home__text">
      Каркас приложения готов: интерфейс, API и локальная база данных запускаются отдельно.
    </p>
  </section>
</template>

<style scoped>
.home__title {
  margin: 0 0 0.5rem;
}

.home__text {
  margin: 0;
}
</style>
