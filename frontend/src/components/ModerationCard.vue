<script setup lang="ts">
import { ref } from 'vue'

import type { ModerationQueueItem } from '../api/client'
import AppButton from './AppButton.vue'

/** Карточка очереди проверки: причина и решение модератора. */
const props = defineProps<{ item: ModerationQueueItem }>()

const emit = defineEmits<{
  (event: 'approve', id: string): void
  (event: 'reject', id: string, reason: string): void
  (event: 'hide', id: string, reason: string): void
}>()

const reason = ref('')

function submitReject(): void {
  const value = reason.value.trim()
  if (value.length > 0) {
    emit('reject', props.item.id, value)
  }
}

function submitHide(): void {
  const value = reason.value.trim()
  if (value.length > 0) {
    emit('hide', props.item.id, value)
  }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('ru-RU')
}
</script>

<template>
  <article class="moderation-card" data-test="queue-item">
    <div class="moderation-card__head">
      <h3 class="moderation-card__title">{{ item.title }}</h3>
      <span class="moderation-card__meta">
        {{ item.authorEmail }} · {{ formatDate(item.submittedAt) }}
      </span>
    </div>

    <label class="field">
      <span class="field__label">Причина отклонения или скрытия</span>
      <textarea v-model="reason" class="input input--textarea" rows="2"></textarea>
    </label>

    <div class="moderation-card__actions">
      <AppButton variant="primary" size="compact" @click="emit('approve', item.id)">
        Одобрить
      </AppButton>
      <AppButton
        variant="danger"
        size="compact"
        :disabled="reason.trim().length === 0"
        @click="submitReject"
      >
        Отклонить
      </AppButton>
      <AppButton
        variant="danger"
        size="compact"
        :disabled="reason.trim().length === 0"
        @click="submitHide"
      >
        Скрыть
      </AppButton>
    </div>
  </article>
</template>