<script setup lang="ts">
import { RouterLink } from 'vue-router'

import EmptyState from '../components/EmptyState.vue'
</script>

<template>
  <section class="page">
    <EmptyState
      title="Страница не найдена"
      description="Такого адреса нет. Проверьте ссылку или вернитесь на главную."
    >
      <template #actions>
        <RouterLink class="btn btn--secondary btn--compact" to="/">На главную</RouterLink>
      </template>
    </EmptyState>
  </section>
</template>
