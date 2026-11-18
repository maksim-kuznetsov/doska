<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { useAppStore } from '../stores/app'

const app = useAppStore()
const isDev = import.meta.env.DEV
</script>

<template>
  <footer class="site-footer">
    <div class="site-footer__inner">
      <span class="site-footer__brand">{{ app.appName }}</span>
      <span>Доска объявлений: подача, поиск и модерация.</span>
      <nav class="site-footer__links" aria-label="Дополнительно">
        <RouterLink to="/ads/new">Подать объявление</RouterLink>
        <RouterLink to="/ads/mine">Мои объявления</RouterLink>
        <RouterLink v-if="isDev" to="/design-system">Дизайн-система</RouterLink>
      </nav>
    </div>
  </footer>
</template>