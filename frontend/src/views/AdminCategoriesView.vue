<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'

import {
  ApiError,
  api,
  type CategoryItemView,
  type CategorySectionView,
} from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import FormGrid from '../components/FormGrid.vue'
import FormGridItem from '../components/FormGridItem.vue'
import FormSection from '../components/FormSection.vue'
import StatusBadge from '../components/StatusBadge.vue'
import TextField from '../components/TextField.vue'

interface Draft {
  name: string
  sortOrder: string
}

const sections = ref<CategorySectionView[]>([])
const drafts = ref<Record<string, Draft>>({})
const newSubsections = reactive<Record<string, Draft>>({})
const newSectionName = ref('')
const newSectionOrder = ref('')
const error = ref('')
const notice = ref('')
const loading = ref(false)

function messageOf(cause: unknown, fallback: string): string {
  return cause instanceof ApiError ? cause.message : fallback
}

function rebuildDrafts(): void {
  const next: Record<string, Draft> = {}

  for (const section of sections.value) {
    next[section.id] = { name: section.name, sortOrder: String(section.sortOrder) }
    newSubsections[section.id] ??= { name: '', sortOrder: '' }

    for (const subsection of section.subsections) {
      next[subsection.id] = {
        name: subsection.name,
        sortOrder: String(subsection.sortOrder),
      }
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
  return drafts.value[id] ?? { name: '', sortOrder: '' }
}

function createSection(): void {
  const name = newSectionName.value.trim()

  if (name.length === 0) {
    return
  }

  void run(async () => {
    await api.adminCategories.createSection(name, Number(newSectionOrder.value) || 0)
    newSectionName.value = ''
    newSectionOrder.value = ''
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
      Number(draft.sortOrder) || 0,
    )
    draft.name = ''
    draft.sortOrder = ''
  }, 'Подраздел создан.')
}

function save(id: string): void {
  const draft = draftFor(id)
  void run(
    () => api.adminCategories.update(id, draft.name.trim(), Number(draft.sortOrder) || 0),
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
    <Breadcrumbs :items="[{ label: 'Главная', to: '/' }, { label: 'Категории' }]" />

    <div class="page__head">
      <h1 class="page__title">Категории</h1>
      <span class="text-muted">Управление доступно администратору</span>
    </div>

    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <form @submit.prevent="createSection">
      <FormSection title="Новый раздел">
        <FormGrid>
          <FormGridItem>
            <TextField
              v-model="newSectionName"
              label="Название"
              input-id="new-section-name"
              required
            />
          </FormGridItem>
          <FormGridItem>
            <TextField
              v-model="newSectionOrder"
              label="Порядок"
              input-id="new-section-order"
              type="number"
              min="0"
            />
          </FormGridItem>
        </FormGrid>
        <div>
          <AppButton variant="primary" type="submit">Создать раздел</AppButton>
        </div>
      </FormSection>
    </form>

    <p v-if="loading" class="text-muted">Загружаем…</p>

    <div class="item-list">
      <FormSection v-for="section in sections" :key="section.id" :title="section.name">
        <div class="split">
          <StatusBadge :status="section.isActive ? 'Active' : 'Archived'" />
          <span class="text-muted">Раздел верхнего уровня</span>
        </div>

        <FormGrid>
          <FormGridItem>
            <TextField
              v-model="drafts[section.id].name"
              label="Название"
              :input-id="`section-name-${section.id}`"
            />
          </FormGridItem>
          <FormGridItem>
            <TextField
              v-model="drafts[section.id].sortOrder"
              label="Порядок"
              :input-id="`section-order-${section.id}`"
              type="number"
              min="0"
            />
          </FormGridItem>
        </FormGrid>

        <div class="cluster">
          <AppButton variant="secondary" size="compact" @click="save(section.id)">
            Сохранить
          </AppButton>
          <AppButton variant="secondary" size="compact" @click="toggle(section)">
            {{ section.isActive ? 'Деактивировать' : 'Активировать' }}
          </AppButton>
          <AppButton variant="danger" size="compact" @click="remove(section)">Удалить</AppButton>
        </div>

        <div class="item-list">
          <div
            v-for="subsection in section.subsections"
            :key="subsection.id"
            class="panel stack stack--compact"
            data-test="admin-subsection"
          >
            <div class="split">
              <span class="item-row__title">{{ subsection.name }}</span>
              <StatusBadge :status="subsection.isActive ? 'Active' : 'Archived'" />
            </div>

            <FormGrid>
              <FormGridItem>
                <TextField
                  v-model="drafts[subsection.id].name"
                  label="Название"
                  :input-id="`subsection-name-${subsection.id}`"
                />
              </FormGridItem>
              <FormGridItem>
                <TextField
                  v-model="drafts[subsection.id].sortOrder"
                  label="Порядок"
                  :input-id="`subsection-order-${subsection.id}`"
                  type="number"
                  min="0"
                />
              </FormGridItem>
            </FormGrid>

            <div class="cluster">
              <AppButton variant="secondary" size="compact" @click="save(subsection.id)">
                Сохранить
              </AppButton>
              <AppButton variant="secondary" size="compact" @click="toggle(subsection)">
                {{ subsection.isActive ? 'Деактивировать' : 'Активировать' }}
              </AppButton>
              <AppButton variant="danger" size="compact" @click="remove(subsection)">
                Удалить
              </AppButton>
            </div>
          </div>
        </div>

        <form @submit.prevent="createSubsection(section.id)">
          <FormGrid>
            <FormGridItem>
              <TextField
                v-model="newSubsections[section.id].name"
                label="Новый подраздел"
                :input-id="`new-subsection-name-${section.id}`"
              />
            </FormGridItem>
            <FormGridItem>
              <TextField
                v-model="newSubsections[section.id].sortOrder"
                label="Порядок"
                :input-id="`new-subsection-order-${section.id}`"
                type="number"
                min="0"
              />
            </FormGridItem>
          </FormGrid>
          <div>
            <AppButton variant="secondary" type="submit">Добавить подраздел</AppButton>
          </div>
        </form>
      </FormSection>
    </div>
  </section>
</template>