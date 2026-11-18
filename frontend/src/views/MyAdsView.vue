<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type AdSummary } from '../api/client'
import ActionLink from '../components/ActionLink.vue'
import AdRow from '../components/AdRow.vue'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import EmptyState from '../components/EmptyState.vue'

const ads = ref<AdSummary[]>([])
const error = ref('')
const notice = ref('')
const loading = ref(false)

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
      <h1 class="page__title">Мои объявления</h1>
      <ActionLink to="/ads/new">Подать объявление</ActionLink>
    </div>

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <EmptyState
      v-if="!loading && !error && ads.length === 0"
      title="Объявлений пока нет"
      description="Подайте первое объявление — оно появится здесь со своим статусом."
    />

    <div v-if="ads.length > 0" class="ad-list">
      <AdRow v-for="ad in ads" :key="ad.id" :ad="ad">
        <template #actions>
          <RouterLink class="btn btn--secondary btn--compact" :to="`/ads/${ad.id}/edit`">
            Редактировать
          </RouterLink>
          <AppButton v-if="canSell(ad.status)" size="compact" variant="secondary" @click="markSold(ad)">
            Продано
          </AppButton>
          <AppButton
            v-if="canReturn(ad.status)"
            size="compact"
            variant="secondary"
            @click="returnToSale(ad)"
          >
            Вернуть в продажу
          </AppButton>
          <AppButton v-if="canRenew(ad.status)" size="compact" variant="secondary" @click="renew(ad)">
            Продлить
          </AppButton>
          <AppButton size="compact" variant="danger" @click="remove(ad)">Удалить</AppButton>
        </template>
      </AdRow>
    </div>
  </section>
</template>