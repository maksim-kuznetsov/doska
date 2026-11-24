<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'

import { api, type ComplaintQueueItem } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import EmptyState from '../components/EmptyState.vue'
import TextAreaField from '../components/TextAreaField.vue'

const complaints = ref<ComplaintQueueItem[]>([])
const hideReasons = reactive<Record<string, string>>({})
const error = ref('')
const notice = ref('')
const loading = ref(false)

async function load(): Promise<void> {
  loading.value = true

  try {
    complaints.value = await api.complaints.queue()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить жалобы.')
  } finally {
    loading.value = false
  }
}

function hideReasonFor(id: string): string {
  return hideReasons[id] ?? ''
}

function setHideReason(id: string, value: string): void {
  hideReasons[id] = value
}

async function resolve(id: string, hideAd: boolean): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await api.complaints.resolve(id, hideAd, hideAd ? hideReasonFor(id).trim() : null)
    notice.value = hideAd
      ? 'Объявление скрыто, жалоба закрыта.'
      : 'Объявление оставлено, жалоба закрыта.'
    await load()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось закрыть жалобу.')
  }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('ru-RU')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Жалобы на объявления</h1>
    </div>

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <EmptyState
      v-if="!loading && complaints.length === 0"
      title="Открытых жалоб нет"
      description="Жалобы пользователей на объявления появятся здесь."
    />

    <div v-if="complaints.length > 0" class="item-list">
      <article v-for="complaint in complaints" :key="complaint.id" class="panel stack">
        <div class="split">
          <h2 class="item-row__title">{{ complaint.adTitle }}</h2>
          <span class="text-muted">{{ formatDate(complaint.createdAt) }}</span>
        </div>
        <p>Жалоба от {{ complaint.authorEmail }}</p>
        <p>Причина: {{ complaint.reason }}</p>

        <TextAreaField
          :model-value="hideReasonFor(complaint.id)"
          :input-id="`hide-reason-${complaint.id}`"
          label="Причина скрытия объявления"
          :rows="2"
          @update:model-value="setHideReason(complaint.id, $event)"
        />

        <div class="cluster">
          <AppButton variant="secondary" size="compact" @click="resolve(complaint.id, false)">
            Оставить объявление
          </AppButton>
          <AppButton
            variant="danger"
            size="compact"
            :disabled="hideReasonFor(complaint.id).trim().length === 0"
            @click="resolve(complaint.id, true)"
          >
            Скрыть объявление
          </AppButton>
        </div>
      </article>
    </div>
  </section>
</template>