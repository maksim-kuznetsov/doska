<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type CityAdminView } from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'
import FormSection from '../components/FormSection.vue'
import StatusBadge from '../components/StatusBadge.vue'
import TextField from '../components/TextField.vue'

const cities = ref<CityAdminView[]>([])
const drafts = ref<Record<string, string>>({})
const newName = ref('')
const error = ref('')
const notice = ref('')
const loading = ref(false)

function messageOf(cause: unknown, fallback: string): string {
  return cause instanceof ApiError ? cause.message : fallback
}

function rebuildDrafts(): void {
  const next: Record<string, string> = {}

  for (const city of cities.value) {
    next[city.id] = city.name
  }

  drafts.value = next
}

async function load(): Promise<void> {
  loading.value = true

  try {
    cities.value = await api.adminCities.list()
    rebuildDrafts()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить города.')
  } finally {
    loading.value = false
  }
}

async function run(action: () => Promise<unknown>, success: string): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await action()
    notice.value = success
    await load()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось выполнить действие.')
  }
}

function create(): void {
  const name = newName.value.trim()

  if (name.length === 0) {
    return
  }

  void run(async () => {
    await api.adminCities.create(name)
    newName.value = ''
  }, 'Город добавлен.')
}

function save(city: CityAdminView): void {
  void run(
    () => api.adminCities.rename(city.id, (drafts.value[city.id] ?? city.name).trim()),
    'Город переименован.',
  )
}

function toggle(city: CityAdminView): void {
  void run(
    () => api.adminCities.setActive(city.id, !city.isActive),
    city.isActive ? 'Город деактивирован.' : 'Город активирован.',
  )
}

function remove(city: CityAdminView): void {
  if (!window.confirm(`Удалить город «${city.name}»?`)) {
    return
  }

  void run(() => api.adminCities.remove(city.id), 'Город удалён.')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <Breadcrumbs :items="[{ label: 'Главная', to: '/' }, { label: 'Города' }]" />

    <div class="page__head">
      <h1 class="page__title">Города</h1>
      <span class="text-muted">Управление доступно администратору</span>
    </div>

    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <form @submit.prevent="create">
      <FormSection title="Новый город" description="Название должно быть уникальным.">
        <TextField
          v-model="newName"
          label="Название"
          input-id="new-city-name"
          :maxlength="200"
          required
        />
        <div>
          <AppButton variant="primary" type="submit">Добавить город</AppButton>
        </div>
      </FormSection>
    </form>

    <p v-if="loading" class="text-muted">Загружаем…</p>

    <EmptyState
      v-if="!loading && cities.length === 0"
      title="Городов пока нет"
      description="Добавьте первый город, чтобы его можно было выбрать при подаче объявления."
    />

    <div v-if="cities.length > 0" class="ad-list">
      <FormSection v-for="city in cities" :key="city.id" :title="city.name">
        <div class="split">
          <StatusBadge :status="city.isActive ? 'Active' : 'Archived'" />
          <span class="text-muted">
            {{ city.isActive ? 'Доступен при подаче' : 'Скрыт из выбора' }}
          </span>
        </div>

        <TextField
          v-model="drafts[city.id]"
          label="Название"
          :input-id="`city-name-${city.id}`"
          :maxlength="200"
        />

        <div class="cluster">
          <AppButton variant="secondary" size="compact" @click="save(city)">Сохранить</AppButton>
          <AppButton variant="secondary" size="compact" @click="toggle(city)">
            {{ city.isActive ? 'Деактивировать' : 'Активировать' }}
          </AppButton>
          <AppButton variant="danger" size="compact" @click="remove(city)">Удалить</AppButton>
        </div>
      </FormSection>
    </div>
  </section>
</template>