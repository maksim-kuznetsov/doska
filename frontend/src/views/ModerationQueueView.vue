<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type ModerationQueueItem } from '../api/client'
import ModerationQueueList from '../components/ModerationQueueList.vue'

const items = ref<ModerationQueueItem[]>([])
const error = ref('')
const notice = ref('')
const loading = ref(false)

async function load(): Promise<void> {
  loading.value = true

  try {
    items.value = await api.moderation.queue()
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить очередь.'
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

function approve(id: string): void {
  void run(() => api.moderation.approve(id), 'Объявление одобрено и опубликовано.')
}

function reject(id: string, reason: string): void {
  void run(() => api.moderation.reject(id, reason), 'Объявление отклонено, автор получил письмо.')
}

function hide(id: string, reason: string): void {
  void run(() => api.moderation.hide(id, reason), 'Объявление скрыто, автор получил письмо.')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <h1>Очередь проверки</h1>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="notice" class="notice">{{ notice }}</p>
    <p v-if="error" class="error">{{ error }}</p>

    <ModerationQueueList
      :items="items"
      @approve="approve"
      @reject="reject"
      @hide="hide"
    />
  </section>
</template>