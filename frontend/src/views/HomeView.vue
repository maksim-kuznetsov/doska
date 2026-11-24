<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import {
  api,
  type AdCard as AdCardModel,
  type CityOption,
  type SectionOption,
} from '../api/client'
import { messageOf } from '../api/errors'
import {
  defaultFeedFilters,
  readFilters,
  toRouteQuery,
  toSearchParams,
  type FeedFilters,
} from '../api/search'
import AppMessage from '../components/AppMessage.vue'
import EmptyState from '../components/EmptyState.vue'
import FilterPanel from '../components/FilterPanel.vue'
import ItemCard from '../components/ItemCard.vue'
import Pagination from '../components/Pagination.vue'
import PromoBanner from '../components/PromoBanner.vue'
import ResultsBar from '../components/ResultsBar.vue'

const route = useRoute()
const router = useRouter()

const filters = reactive<FeedFilters>({ ...defaultFeedFilters })
const items = ref<AdCardModel[]>([])
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
    error.value = messageOf(cause, 'Не удалось загрузить объявления.')
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

function applySort(): void {
  applyFilters()
}

function goToPage(page: number): void {
  void router.push({ name: 'home', query: toRouteQuery({ ...filters, page }) })
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
    <div class="page__head">
      <div class="stack stack--compact">
        <h1 class="page__title">Объявления</h1>
        <p class="page__subtitle">
          Публичная лента активных объявлений: поиск, фильтры и сортировка.
        </p>
      </div>
    </div>

    <PromoBanner />

    <FilterPanel
      :query="filters.query"
      :category-id="filters.categoryId"
      :city-id="filters.cityId"
      :min-price="filters.minPrice"
      :max-price="filters.maxPrice"
      :sort="filters.sort"
      :sections="sections"
      :cities="cities"
      @update:query="filters.query = $event"
      @update:category-id="filters.categoryId = $event"
      @update:city-id="filters.cityId = $event"
      @update:min-price="filters.minPrice = $event"
      @update:max-price="filters.maxPrice = $event"
      @submit="applyFilters"
      @reset="resetFilters"
    />

    <ResultsBar :total="totalCount" :sort="filters.sort" @update:sort="applySort" />

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <EmptyState
      v-if="!loading && !error && items.length === 0"
      title="Ничего не найдено"
      description="Фильтры сохранены — измените условия или сбросьте их."
    />

    <div v-if="items.length > 0" class="item-grid">
      <ItemCard v-for="ad in items" :key="ad.id" :ad="ad" />
    </div>

    <Pagination
      v-if="totalPages > 1"
      :page="filters.page"
      :total-pages="totalPages"
      :total-count="totalCount"
      @change="goToPage"
    />
  </section>
</template>