<script setup lang="ts">
import { reactive } from 'vue'

import type { ModerationQueueItem } from '../api/client'

/** Очередь проверки: показывает объявления и отправляет решение родительскому представлению. */
defineProps<{
  items: ModerationQueueItem[]
}>()

const emit = defineEmits<{
  (event: 'approve', id: string): void
  (event: 'reject', id: string, reason: string): void
  (event: 'hide', id: string, reason: string): void
}>()

const reasons = reactive<Record<string, string>>({})

function reasonFor(id: string): string {
  return reasons[id] ?? ''
}

function setReason(id: string, value: string): void {
  reasons[id] = value
}

function onReasonInput(id: string, event: Event): void {
  const target = event.target as HTMLTextAreaElement
  setReason(id, target.value)
}

function reject(id: string): void {
  const reason = reasonFor(id).trim()
  if (reason.length > 0) {
    emit('reject', id, reason)
  }
}

function hide(id: string): void {
  const reason = reasonFor(id).trim()
  if (reason.length > 0) {
    emit('hide', id, reason)
  }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('ru-RU')
}
</script>

<template>
  <p v-if="items.length === 0" class="muted">Очередь проверки пуста.</p>

  <ul v-else class="queue">
    <li v-for="item in items" :key="item.id" class="queue__item" data-test="queue-item">
      <div class="queue__head">
        <h3 class="queue__title">{{ item.title }}</h3>
        <span class="muted">{{ item.authorEmail }} · {{ formatDate(item.submittedAt) }}</span>
      </div>

      <label class="field">
        <span class="field__label">Причина отклонения или скрытия</span>
        <textarea
          class="field__input"
          rows="2"
          :value="reasonFor(item.id)"
          @input="onReasonInput(item.id, $event)"
        ></textarea>
      </label>

      <div class="actions">
        <button type="button" class="button" @click="emit('approve', item.id)">Одобрить</button>
        <button
          type="button"
          class="button button--danger"
          :disabled="reasonFor(item.id).trim().length === 0"
          @click="reject(item.id)"
        >
          Отклонить
        </button>
        <button
          type="button"
          class="button button--danger"
          :disabled="reasonFor(item.id).trim().length === 0"
          @click="hide(item.id)"
        >
          Скрыть
        </button>
      </div>
    </li>
  </ul>
</template>

<style scoped>
.queue {
  margin: 0;
  padding: 0;
  list-style: none;
  display: grid;
  gap: 1rem;
}

.queue__item {
  border: 1px solid currentColor;
  border-radius: 0.5rem;
  padding: 1rem;
  display: grid;
  gap: 0.75rem;
}

.queue__head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.5rem;
}

.queue__title {
  margin: 0;
  font-size: 1.1rem;
}
</style>