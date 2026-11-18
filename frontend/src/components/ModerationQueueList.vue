<script setup lang="ts">
import type { ModerationQueueItem } from '../api/client'
import EmptyState from './EmptyState.vue'
import ModerationCard from './ModerationCard.vue'

/** Очередь проверки: показывает объявления и отправляет решение родительскому представлению. */
defineProps<{
  items: ModerationQueueItem[]
}>()

const emit = defineEmits<{
  (event: 'approve', id: string): void
  (event: 'reject', id: string, reason: string): void
  (event: 'hide', id: string, reason: string): void
}>()
</script>

<template>
  <EmptyState
    v-if="items.length === 0"
    title="Очередь проверки пуста"
    description="Новые объявления появятся здесь автоматически."
  />

  <div v-else class="moderation-list">
    <ModerationCard
      v-for="item in items"
      :key="item.id"
      :item="item"
      @approve="emit('approve', $event)"
      @reject="(id, reason) => emit('reject', id, reason)"
      @hide="(id, reason) => emit('hide', id, reason)"
    />
  </div>
</template>