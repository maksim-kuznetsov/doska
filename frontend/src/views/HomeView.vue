<script setup lang="ts">
import { storeToRefs } from 'pinia'

import { useAppStore } from '../stores/app'

const { appName } = storeToRefs(useAppStore())
</script>

<template>
  <section class="page">
    <h1 class="home__title">{{ appName }}</h1>
    <p class="home__text">
      Доска объявлений. Подайте объявление — после проверки модератором оно появится в ленте.
      Доверенные авторы публикуются сразу, а решения по объявлениям приходят на email.
    </p>

    <div class="actions">
      <RouterLink class="button button--primary" to="/ads/new">Подать объявление</RouterLink>
      <RouterLink class="button" to="/ads/mine">Мои объявления</RouterLink>
    </div>
  </section>
</template>

<style scoped>
.home__title {
  margin: 0 0 0.5rem;
}

.home__text {
  margin: 0 0 1rem;
  max-width: 40rem;
}
</style>