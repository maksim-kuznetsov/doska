<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { api, type ModerationQueueItem } from '../api/client'
import { messageOf } from '../api/errors'
import AppMessage from '../components/AppMessage.vue'
import ModerationQueueList from '../components/ModerationQueueList.vue'
import { useApiAction } from '../composables/useApiAction'

const items = ref<ModerationQueueItem[]>([])
const { error, notice, run } = useApiAction(load)
const loading = ref(false)

async function load(): Promise<void> {
  loading.value = true

  try {
    items.value = await api.moderation.queue()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить очередь.')
  } finally {
    loading.value = false
  }
}

function approve(id: string): void {
  void run(
    () => api.moderation.approve(id),
    'Объявление одобрено и опубликовано.',
    'Не удалось выполнить действие.',
  )
}

function reject(id: string, reason: string): void {
  void run(
    () => api.moderation.reject(id, reason),
    'Объявление отклонено, автор получил письмо.',
    'Не удалось выполнить действие.',
  )
}

function hide(id: string, reason: string): void {
  void run(
    () => api.moderation.hide(id, reason),
    'Объявление скрыто, автор получил письмо.',
    'Не удалось выполнить действие.',
  )
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Очередь проверки</h1>
    </div>

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <ModerationQueueList :items="items" @approve="approve" @reject="reject" @hide="hide" />
  </section>
</template>