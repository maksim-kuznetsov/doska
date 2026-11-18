<script setup lang="ts">
import AppButton from './AppButton.vue'

/** Постраничная навигация ленты. */
const props = defineProps<{
  page: number
  totalPages: number
  totalCount?: number
}>()

const emit = defineEmits<{ (event: 'change', page: number): void }>()

function go(page: number): void {
  if (page < 1 || page > props.totalPages || page === props.page) {
    return
  }

  emit('change', page)
}
</script>

<template>
  <nav class="pagination" aria-label="Постраничная навигация">
    <AppButton variant="secondary" size="compact" :disabled="page <= 1" @click="go(page - 1)">
      Назад
    </AppButton>
    <span class="pagination__status">
      Страница {{ page }} из {{ totalPages }}
      <template v-if="totalCount !== undefined"> · всего {{ totalCount }}</template>
    </span>
    <AppButton
      variant="secondary"
      size="compact"
      :disabled="page >= totalPages"
      @click="go(page + 1)"
    >
      Вперёд
    </AppButton>
  </nav>
</template>