<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import {
  ApiError,
  api,
  type AdCard,
  type CityOption,
  type SectionOption,
} from '../api/client'
import {
  defaultFeedFilters,
  readFilters,
  toRouteQuery,
  toSearchParams,
  type FeedFilters,
} from '../api/search'
import { useAppStore } from '../stores/app'

const { appName } = storeToRefs(useAppStore())
const route = useRoute()
const router = useRouter()

const filters = reactive<FeedFilters>({ ...defaultFeedFilters })
const items = ref<AdCard[]>([])
const totalCount = ref(0)
const pageSize = ref(12)
const loading = ref(false)
const error = ref('')
const cities = ref<CityOption[]>([])
const sections = ref<SectionOption[]>([])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function syncFromRoute(): void {
  Object.assign(filters, readFilters(route.query as Record<string, unknown>))
}

async function load(): Promise<void> {
  loading.value = true
  error.value = ''

  try {
    const result = await api.ads.search(toSearchParams(filters))
    items.value = result.items
    totalCount.value = result.totalCount
    pageSize.value = result.pageSize
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявления.'
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

function applyFilters(): void {
  void router.push({ name: 'home', query: toRouteQuery({ ...filters, page: 1 }) })
}

function resetFilters(): void {
  void router.push({ name: 'home', query: {} })
}

function goToPage(page: number): void {
  if (page < 1 || page > totalPages.value) {
    return
  }

  void router.push({ name: 'home', query: toRouteQuery({ ...filters, page }) })
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('ru-RU')
}

watch(
  () => route.query,
  async () => {
    syncFromRoute()
    await load()
  },
)

onMounted(async () => {
  syncFromRoute()

  await Promise.all([
    load(),
    api.catalog.cities().then(
      (list) => {
        cities.value = list
      },
      () => undefined,
    ),
    api.catalog.sections().then(
      (list) => {
        sections.value = list
      },
      () => undefined,
    ),
  ])
})
</script>

<template>
  <section class="page">
    <h1 class="home__title">{{ appName }}</h1>
    <p class="muted">Публичная лента активных объявлений: поиск, фильтры и сортировка.</p>

    <form class="filters" @submit.prevent="applyFilters">
      <label class="field field--wide">
        <span class="field__label">Поиск</span>
        <input
          v-model="filters.query"
          class="field__input"
          type="search"
          placeholder="Что ищем?"
        />
      </label>

      <label class="field">
        <span class="field__label">Раздел</span>
        <select v-model="filters.categoryId" class="field__input">
          <option value="">Все разделы</option>
          <optgroup v-for="section in sections" :key="section.id" :label="section.name">
            <option :value="section.id">Все: {{ section.name }}</option>
            <option v-for="sub in section.subsections" :key="sub.id" :value="sub.id">
              {{ sub.name }}
            </option>
          </optgroup>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Город</span>
        <select v-model="filters.cityId" class="field__input">
          <option value="">Все города</option>
          <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Цена от</span>
        <input v-model="filters.minPrice" class="field__input" type="number" min="0" />
      </label>

      <label class="field">
        <span class="field__label">Цена до</span>
        <input v-model="filters.maxPrice" class="field__input" type="number" min="0" />
      </label>

      <label class="field">
        <span class="field__label">Сортировка</span>
        <select v-model="filters.sort" class="field__input">
          <option value="newest">Сначала новые</option>
          <option value="price_asc">Цена: по возрастанию</option>
          <option value="price_desc">Цена: по убыванию</option>
        </select>
      </label>

      <div class="actions">
        <button class="button button--primary" type="submit">Показать</button>
        <button class="button" type="button" @click="resetFilters">Сбросить</button>
      </div>
    </form>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="error" class="error">{{ error }}</p>

    <p v-if="!loading && !error && items.length === 0" class="muted" data-test="empty">
      Ничего не найдено. Фильтры сохранены — измените условия или сбросьте их.
    </p>

    <ul v-if="items.length > 0" class="list feed">
      <li v-for="ad in items" :key="ad.id" class="card" data-test="ad-card">
        <div class="ad-card__media">
          <img v-if="ad.thumbnailUrl" :src="ad.thumbnailUrl" :alt="ad.title" />
          <div v-else class="ad-card__placeholder" aria-hidden="true">Нет фото</div>
        </div>

        <div class="card__head">
          <h2 class="card__title">{{ ad.title }}</h2>
          <span class="badge">{{ ad.price }} ₽</span>
        </div>
        <p class="card__text muted">
          {{ ad.sectionName }} — {{ ad.categoryName }} · {{ ad.cityName }}
        </p>
        <p class="card__text">{{ ad.description }}</p>
        <p class="card__text muted">{{ formatDate(ad.publishedAt) }}</p>
      </li>
    </ul>

    <div v-if="totalPages > 1" class="pagination">
      <button
        class="button"
        type="button"
        :disabled="filters.page <= 1"
        @click="goToPage(filters.page - 1)"
      >
        Назад
      </button>
      <span class="muted">Страница {{ filters.page }} из {{ totalPages }} · всего {{ totalCount }}</span>
      <button
        class="button"
        type="button"
        :disabled="filters.page >= totalPages"
        @click="goToPage(filters.page + 1)"
      >
        Вперёд
      </button>
    </div>
  </section>
</template>

<style scoped>
.home__title {
  margin: 0;
}

.filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 0.75rem;
  align-items: end;
}

.field--wide {
  grid-column: 1 / -1;
}

.pagination {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.ad-card__media {
  width: 100%;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  border-radius: 0.4rem;
  border: 1px solid currentColor;
}

.ad-card__media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.ad-card__placeholder {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  font-size: 0.85rem;
  opacity: 0.6;
}
</style>