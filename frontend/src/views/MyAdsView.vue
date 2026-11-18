<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type AdSummary } from '../api/client'

const ads = ref<AdSummary[]>([])
const error = ref('')
const notice = ref('')
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

async function load(): Promise<void> {
  loading.value = true

  try {
    ads.value = await api.ads.mine()
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявления.'
  } finally {
    loading.value = false
  }
}

async function run(action: () => Promise<unknown>, message: string): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await action()
    notice.value = message
    await load()
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось выполнить действие.'
  }
}

function markSold(ad: AdSummary): void {
  void run(() => api.ads.markSold(ad.id), 'Объявление отмечено проданным.')
}

function returnToSale(ad: AdSummary): void {
  void run(() => api.ads.returnToSale(ad.id), 'Объявление снова в продаже.')
}

function renew(ad: AdSummary): void {
  void run(() => api.ads.renew(ad.id), 'Объявление продлено на 7 дней.')
}

function remove(ad: AdSummary): void {
  if (!window.confirm(`Удалить «${ad.title}»?`)) {
    return
  }

  void run(() => api.ads.remove(ad.id), 'Объявление удалено.')
}

function canSell(status: string): boolean {
  return status === 'Active'
}

function canReturn(status: string): boolean {
  return status === 'Sold'
}

function canRenew(status: string): boolean {
  return status === 'Active' || status === 'Archived'
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1>Мои объявления</h1>
      <RouterLink class="button button--primary" to="/ads/new">Подать объявление</RouterLink>
    </div>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="notice" class="notice">{{ notice }}</p>
    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="!loading && !error && ads.length === 0" class="muted">Объявлений пока нет.</p>

    <ul v-if="ads.length > 0" class="list">
      <li v-for="ad in ads" :key="ad.id" class="card" data-test="my-ad">
        <div class="card__head">
          <h2 class="card__title">{{ ad.title }}</h2>
          <span class="badge">{{ statusLabel(ad.status) }}</span>
        </div>

        <div class="ad-row">
          <img
            v-if="ad.photos.length > 0"
            class="ad-thumb"
            :src="ad.photos[0].thumbnailUrl"
            :alt="ad.title"
          />
          <div v-else class="ad-thumb ad-thumb--empty" aria-hidden="true">Нет фото</div>

          <div class="ad-info">
            <p class="card__text">{{ ad.price }} ₽ · {{ ad.contacts }}</p>
            <p v-if="ad.moderationReason" class="error">Причина: {{ ad.moderationReason }}</p>
          </div>
        </div>

        <div class="actions">
          <RouterLink class="button" :to="`/ads/${ad.id}/edit`">Редактировать</RouterLink>
          <button v-if="canSell(ad.status)" type="button" class="button" @click="markSold(ad)">
            Продано
          </button>
          <button v-if="canReturn(ad.status)" type="button" class="button" @click="returnToSale(ad)">
            Вернуть в продажу
          </button>
          <button v-if="canRenew(ad.status)" type="button" class="button" @click="renew(ad)">
            Продлить
          </button>
          <button type="button" class="button button--danger" @click="remove(ad)">Удалить</button>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.ad-row {
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;
}

.ad-thumb {
  width: 8rem;
  height: 6rem;
  object-fit: cover;
  border-radius: 0.4rem;
  border: 1px solid currentColor;
}

.ad-thumb--empty {
  display: grid;
  place-items: center;
  font-size: 0.8rem;
  opacity: 0.6;
}

.ad-info {
  display: grid;
  gap: 0.35rem;
}
</style>