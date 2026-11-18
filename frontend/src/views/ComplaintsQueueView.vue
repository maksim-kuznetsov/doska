<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'

import { ApiError, api, type ComplaintQueueItem } from '../api/client'

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
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить жалобы.'
  } finally {
    loading.value = false
  }
}

function hideReasonFor(id: string): string {
  return hideReasons[id] ?? ''
}

function onReasonInput(id: string, event: Event): void {
  const target = event.target as HTMLTextAreaElement
  hideReasons[id] = target.value
}

async function resolve(id: string, hideAd: boolean): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await api.complaints.resolve(id, hideAd, hideAd ? hideReasonFor(id).trim() : null)
    notice.value = hideAd ? 'Объявление скрыто, жалоба закрыта.' : 'Объявление оставлено, жалоба закрыта.'
    await load()
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось закрыть жалобу.'
  }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('ru-RU')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <h1>Жалобы на объявления</h1>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="notice" class="notice">{{ notice }}</p>
    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="!loading && complaints.length === 0" class="muted">Открытых жалоб нет.</p>

    <ul v-if="complaints.length > 0" class="list">
      <li v-for="complaint in complaints" :key="complaint.id" class="card">
        <div class="card__head">
          <h2 class="card__title">{{ complaint.adTitle }}</h2>
          <span class="muted">{{ formatDate(complaint.createdAt) }}</span>
        </div>
        <p class="card__text">Жалоба от {{ complaint.authorEmail }}</p>
        <p class="card__text">Причина: {{ complaint.reason }}</p>

        <label class="field">
          <span class="field__label">Причина скрытия объявления</span>
          <textarea
            class="field__input"
            rows="2"
            :value="hideReasonFor(complaint.id)"
            @input="onReasonInput(complaint.id, $event)"
          ></textarea>
        </label>

        <div class="actions">
          <button type="button" class="button" @click="resolve(complaint.id, false)">
            Оставить объявление
          </button>
          <button
            type="button"
            class="button button--danger"
            :disabled="hideReasonFor(complaint.id).trim().length === 0"
            @click="resolve(complaint.id, true)"
          >
            Скрыть объявление
          </button>
        </div>
      </li>
    </ul>
  </section>
</template>