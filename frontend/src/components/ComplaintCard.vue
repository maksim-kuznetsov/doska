<script setup lang="ts">
import { ref } from 'vue'

import type { ComplaintQueueItem } from '../api/client'
import AdminAdPreview from './AdminAdPreview.vue'
import AppButton from './AppButton.vue'
import TextAreaField from './TextAreaField.vue'

/** Карточка очереди жалоб: данные объявления, сведения о жалобе и решение модератора. */
const props = defineProps<{ complaint: ComplaintQueueItem }>()

const emit = defineEmits<{
  (event: 'resolve', id: string, hideAd: boolean, hideReason: string | null): void
}>()

const hideReason = ref('')

function formatDate(value: string): string {
  return new Date(value).toLocaleString('ru-RU')
}

function leave(): void {
  emit('resolve', props.complaint.id, false, null)
}

function hide(): void {
  const value = hideReason.value.trim()
  if (value.length > 0) {
    emit('resolve', props.complaint.id, true, value)
  }
}
</script>

<template>
  <article class="moderation-card" data-test="complaint-item">
    <div class="moderation-card__head">
      <h3 class="moderation-card__title">{{ complaint.adTitle }}</h3>
      <span class="moderation-card__meta">Автор объявления: {{ complaint.adAuthorEmail }}</span>
    </div>

    <AdminAdPreview
      :photos="complaint.adPhotos"
      :price="complaint.adPrice"
      :city-name="complaint.adCityName"
      :category-name="complaint.adCategoryName"
      :description="complaint.adDescription"
      :contacts="complaint.adContacts"
      :status="complaint.adStatus"
    />

    <div class="stack stack--compact">
      <p>Жалоба от {{ complaint.authorEmail }}</p>
      <p>Причина: {{ complaint.reason }}</p>
      <p class="text-muted">
        {{ formatDate(complaint.createdAt) }} · открытых жалоб на объявление:
        {{ complaint.openComplaintsCount }}
      </p>
    </div>

    <TextAreaField
      v-model="hideReason"
      :input-id="`hide-reason-${complaint.id}`"
      label="Причина скрытия объявления"
      :rows="2"
    />

    <div class="moderation-card__actions">
      <AppButton variant="secondary" size="compact" @click="leave">
        Оставить объявление
      </AppButton>
      <AppButton
        variant="danger"
        size="compact"
        :disabled="hideReason.trim().length === 0"
        @click="hide"
      >
        Скрыть объявление
      </AppButton>
    </div>
  </article>
</template>
