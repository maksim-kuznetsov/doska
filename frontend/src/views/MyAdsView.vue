<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type AdSummary } from '../api/client'

const ads = ref<AdSummary[]>([])
const error = ref('')
const loading = ref(false)

const statusLabels: Record<string, string> = {
  OnModeration: 'На проверке',
  Active: 'Активно',
  Sold: 'Продано',
  Archived: 'Архив',
  Rejected: 'Отклонено',
  Hidden: 'Скрыто',
}

function statusLabel(status: string): string {
  return statusLabels[status] ?? status
}

onMounted(async () => {
  loading.value = true

  try {
    ads.value = await api.ads.mine()
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявления.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1>Мои объявления</h1>
      <RouterLink class="button button--primary" to="/ads/new">Подать объявление</RouterLink>
    </div>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="!loading && !error && ads.length === 0" class="muted">Объявлений пока нет.</p>

    <ul v-if="ads.length > 0" class="list">
      <li v-for="ad in ads" :key="ad.id" class="card">
        <div class="card__head">
          <h2 class="card__title">{{ ad.title }}</h2>
          <span class="badge">{{ statusLabel(ad.status) }}</span>
        </div>
        <p class="card__text">{{ ad.price }} ₽ · {{ ad.contacts }}</p>
        <p v-if="ad.moderationReason" class="error">
          Причина: {{ ad.moderationReason }}
        </p>
      </li>
    </ul>
  </section>
</template>