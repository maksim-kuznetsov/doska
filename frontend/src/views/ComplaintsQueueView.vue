<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { api, type ComplaintQueueItem } from '../api/client'
import { messageOf } from '../api/errors'
import AppMessage from '../components/AppMessage.vue'
import ComplaintCard from '../components/ComplaintCard.vue'
import EmptyState from '../components/EmptyState.vue'

const complaints = ref<ComplaintQueueItem[]>([])
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

async function resolve(id: string, hideAd: boolean, hideReason: string | null): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await api.complaints.resolve(id, hideAd, hideReason)
    notice.value = hideAd
      ? 'Объявление скрыто, жалоба закрыта.'
      : 'Объявление оставлено, жалоба закрыта.'
    await load()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось закрыть жалобу.')
  }
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
      <ComplaintCard
        v-for="complaint in complaints"
        :key="complaint.id"
        :complaint="complaint"
        @resolve="resolve"
      />
    </div>
  </section>
</template>
