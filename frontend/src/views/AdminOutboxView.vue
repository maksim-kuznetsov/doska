<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { api, type OutboxMessageView } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'

const messages = ref<OutboxMessageView[]>([])
const selected = ref<OutboxMessageView | null>(null)
const content = ref('')
const error = ref('')
const loading = ref(false)
const loadingContent = ref(false)

function formatDate(value: string): string {
  const date = new Date(value)

  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('ru-RU')
}

async function load(): Promise<void> {
  loading.value = true
  error.value = ''

  try {
    messages.value = await api.adminOutbox.list()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить письма.')
  } finally {
    loading.value = false
  }
}

async function open(message: OutboxMessageView): Promise<void> {
  selected.value = message
  content.value = ''
  error.value = ''
  loadingContent.value = true

  try {
    content.value = (await api.adminOutbox.read(message.fileName)).content
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось прочитать письмо.')
  } finally {
    loadingContent.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <Breadcrumbs :items="[{ label: 'Главная', to: '/' }, { label: 'Письма (outbox)' }]" />

    <div class="page__head">
      <h1 class="page__title">Письма (outbox)</h1>
      <span class="text-muted">Временный файловый ящик; доступно администратору</span>
    </div>

    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <p v-if="loading" class="text-muted">Загружаем…</p>

    <EmptyState
      v-if="!loading && messages.length === 0"
      title="Писем нет"
      description="В файловом ящике пока нет сохранённых писем."
    />

    <div v-if="messages.length > 0" class="split">
      <div class="item-list">
        <article
          v-for="message in messages"
          :key="message.fileName"
          class="panel stack"
          data-test="outbox-message"
        >
          <h2 class="item-row__title">{{ message.subject || '(без темы)' }}</h2>
          <p class="text-muted">
            {{ message.recipient || 'получатель неизвестен' }} ·
            {{ formatDate(message.sentAt ?? message.modifiedAt) }}
          </p>
          <div class="cluster">
            <AppButton variant="secondary" size="compact" @click="open(message)">
              Открыть
            </AppButton>
          </div>
        </article>
      </div>

      <section v-if="selected" class="panel stack" data-test="outbox-content">
        <h2 class="item-row__title">{{ selected.subject || '(без темы)' }}</h2>
        <p class="text-muted">{{ selected.recipient }} · {{ selected.fileName }}</p>
        <p v-if="loadingContent" class="text-muted">Загружаем письмо…</p>
        <pre v-else class="outbox-content">{{ content }}</pre>
      </section>
    </div>
  </section>
</template>

<style scoped>
.outbox-content {
  margin: 0;
  font-family: monospace;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
