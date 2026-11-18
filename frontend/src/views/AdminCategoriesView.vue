<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'

import {
  ApiError,
  api,
  type CategoryItemView,
  type CategorySectionView,
} from '../api/client'

interface Draft {
  name: string
  sortOrder: number
}

const sections = ref<CategorySectionView[]>([])
const drafts = ref<Record<string, Draft>>({})
const newSubsections = reactive<Record<string, Draft>>({})
const newSectionName = ref('')
const newSectionOrder = ref(0)
const error = ref('')
const notice = ref('')
const loading = ref(false)

function messageOf(cause: unknown, fallback: string): string {
  return cause instanceof ApiError ? cause.message : fallback
}

function rebuildDrafts(): void {
  const next: Record<string, Draft> = {}

  for (const section of sections.value) {
    next[section.id] = { name: section.name, sortOrder: section.sortOrder }
    newSubsections[section.id] ??= { name: '', sortOrder: 0 }

    for (const subsection of section.subsections) {
      next[subsection.id] = { name: subsection.name, sortOrder: subsection.sortOrder }
    }
  }

  drafts.value = next
}

async function load(): Promise<void> {
  loading.value = true

  try {
    sections.value = await api.adminCategories.tree()
    rebuildDrafts()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить категории.')
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

function draftFor(id: string): Draft {
  return drafts.value[id] ?? { name: '', sortOrder: 0 }
}

function createSection(): void {
  const name = newSectionName.value.trim()

  if (name.length === 0) {
    return
  }

  void run(async () => {
    await api.adminCategories.createSection(name, Number(newSectionOrder.value))
    newSectionName.value = ''
    newSectionOrder.value = 0
  }, 'Раздел создан.')
}

function createSubsection(sectionId: string): void {
  const draft = newSubsections[sectionId]

  if (!draft || draft.name.trim().length === 0) {
    return
  }

  void run(async () => {
    await api.adminCategories.createSubsection(
      sectionId,
      draft.name.trim(),
      Number(draft.sortOrder),
    )
    draft.name = ''
    draft.sortOrder = 0
  }, 'Подраздел создан.')
}

function save(id: string): void {
  const draft = draftFor(id)
  void run(
    () => api.adminCategories.update(id, draft.name.trim(), Number(draft.sortOrder)),
    'Категория сохранена.',
  )
}

function toggle(item: CategoryItemView | CategorySectionView): void {
  void run(
    () => api.adminCategories.setActive(item.id, !item.isActive),
    item.isActive ? 'Категория деактивирована.' : 'Категория активирована.',
  )
}

function remove(item: CategoryItemView | CategorySectionView): void {
  if (!window.confirm(`Удалить «${item.name}»?`)) {
    return
  }

  void run(() => api.adminCategories.remove(item.id), 'Категория удалена.')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1>Категории</h1>
      <span class="muted">Управление доступно администратору</span>
    </div>

    <p v-if="notice" class="notice">{{ notice }}</p>
    <p v-if="error" class="error">{{ error }}</p>

    <form class="form card" @submit.prevent="createSection">
      <h2 class="card__title">Новый раздел</h2>
      <div class="category-row">
        <label class="field">
          <span class="field__label">Название</span>
          <input v-model="newSectionName" class="field__input" type="text" required />
        </label>
        <label class="field field--order">
          <span class="field__label">Порядок</span>
          <input v-model.number="newSectionOrder" class="field__input" type="number" min="0" />
        </label>
        <button class="button button--primary" type="submit">Создать раздел</button>
      </div>
    </form>

    <p v-if="loading" class="muted">Загружаем…</p>

    <ul class="list">
      <li v-for="section in sections" :key="section.id" class="card">
        <div class="card__head">
          <h2 class="card__title">{{ section.name }}</h2>
          <span class="badge">{{ section.isActive ? 'Активен' : 'Неактивен' }}</span>
        </div>

        <div class="category-row">
          <label class="field">
            <span class="field__label">Название</span>
            <input v-model="drafts[section.id].name" class="field__input" type="text" />
          </label>
          <label class="field field--order">
            <span class="field__label">Порядок</span>
            <input
              v-model.number="drafts[section.id].sortOrder"
              class="field__input"
              type="number"
              min="0"
            />
          </label>
          <div class="actions">
            <button type="button" class="button" @click="save(section.id)">Сохранить</button>
            <button type="button" class="button" @click="toggle(section)">
              {{ section.isActive ? 'Деактивировать' : 'Активировать' }}
            </button>
            <button type="button" class="button button--danger" @click="remove(section)">
              Удалить
            </button>
          </div>
        </div>

        <ul class="list">
          <li
            v-for="subsection in section.subsections"
            :key="subsection.id"
            class="subsection"
            data-test="admin-subsection"
          >
            <div class="card__head">
              <span class="category-row__name">{{ subsection.name }}</span>
              <span class="badge">{{ subsection.isActive ? 'Активен' : 'Неактивен' }}</span>
            </div>

            <div class="category-row">
              <label class="field">
                <span class="field__label">Название</span>
                <input v-model="drafts[subsection.id].name" class="field__input" type="text" />
              </label>
              <label class="field field--order">
                <span class="field__label">Порядок</span>
                <input
                  v-model.number="drafts[subsection.id].sortOrder"
                  class="field__input"
                  type="number"
                  min="0"
                />
              </label>
              <div class="actions">
                <button type="button" class="button" @click="save(subsection.id)">Сохранить</button>
                <button type="button" class="button" @click="toggle(subsection)">
                  {{ subsection.isActive ? 'Деактивировать' : 'Активировать' }}
                </button>
                <button
                  type="button"
                  class="button button--danger"
                  @click="remove(subsection)"
                >
                  Удалить
                </button>
              </div>
            </div>
          </li>
        </ul>

        <form class="form category-row" @submit.prevent="createSubsection(section.id)">
          <label class="field">
            <span class="field__label">Новый подраздел</span>
            <input v-model="newSubsections[section.id].name" class="field__input" type="text" />
          </label>
          <label class="field field--order">
            <span class="field__label">Порядок</span>
            <input
              v-model.number="newSubsections[section.id].sortOrder"
              class="field__input"
              type="number"
              min="0"
            />
          </label>
          <button class="button" type="submit">Добавить подраздел</button>
        </form>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.category-row {
  display: flex;
  flex-wrap: wrap;
  align-items: end;
  gap: 0.75rem;
}

.field--order {
  max-width: 8rem;
}

.subsection {
  display: grid;
  gap: 0.5rem;
  padding: 0.75rem;
  border: 1px solid currentColor;
  border-radius: 0.4rem;
}

.category-row__name {
  font-weight: 600;
}
</style>