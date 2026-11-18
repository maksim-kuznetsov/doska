<script setup lang="ts">
import { RouterLink } from 'vue-router'

/** Ссылка-действие с декоративной иконкой «+» перед подписью. */
defineProps<{
  to: string | { name: string; query?: Record<string, string> }
}>()
</script>

<template>
  <RouterLink class="action-link" :to="to">
    <span class="action-link__icon" aria-hidden="true">+</span>
    <span><slot /></span>
  </RouterLink>
</template>