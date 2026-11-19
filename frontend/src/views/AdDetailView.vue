<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

import {
  ApiError,
  api,
  type AdSummary,
  type CityOption,
  type SectionOption,
} from '../api/client'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'
import ImageGallery from '../components/ImageGallery.vue'

/** Публичная страница объявления: галерея, цена, описание и контакты. */
const route = useRoute()
const adId = String(route.params.id)

const ad = ref<AdSummary | null>(null)
const sections = ref<SectionOption[]>([])
const cities = ref<CityOption[]>([])
const loading = ref(true)
const notFound = ref(false)
const error = ref('')

const sectionName = computed(
  () =>
    sections.value.find((section) =>
      section.subsections.some((item) => item.id === ad.value?.categoryId),
    )?.name ?? '',
)

const categoryName = computed(
  () =>
    sections.value
      .flatMap((section) => section.subsections)
      .find((item) => item.id === ad.value?.categoryId)?.name ?? '',
)

const cityName = computed(
  () => cities.value.find((city) => city.id === ad.value?.cityId)?.name ?? '',
)

const crumbs = computed(() => {
  const items: Array<{ label: string; to?: string }> = [{ label: 'Главная', to: '/' }]

  if (sectionName.value) {
    items.push({ label: sectionName.value })
  }

  if (categoryName.value && ad.value) {
    items.push({ label: categoryName.value, to: `/?categoryId=${ad.value.categoryId}` })
  }

  items.push({ label: 'Объявление' })
  return items
})

const publishedDate = computed(() =>
  ad.value
    ? new Date(ad.value.publishedAt ?? ad.value.createdAt).toLocaleDateString('ru-RU')
    : '',
)

onMounted(async () => {
  try {
    const [loaded, sectionList, cityList] = await Promise.all([
      api.ads.get(adId),
      api.catalog.sections().catch(() => [] as SectionOption[]),
      api.catalog.cities().catch(() => [] as CityOption[]),
    ])

    ad.value = loaded
    sections.value = sectionList
    cities.value = cityList
  } catch (cause) {
    if (cause instanceof ApiError && cause.status === 404) {
      notFound.value = true
    } else {
      error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявление.'
    }
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section class="page">
    <Breadcrumbs :items="crumbs" />

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <EmptyState
      v-if="!loading && notFound"
      title="Объявление недоступно"
      description="Оно снято с публикации, продано или не существует."
    >
      <template #actions>
        <RouterLink class="btn btn--secondary btn--compact" to="/">На главную</RouterLink>
      </template>
    </EmptyState>

    <article v-if="ad" class="stack">
      <div class="page__head">
        <h1 class="page__title">{{ ad.title }}</h1>
        <span class="ad-card__price">{{ ad.price }} ₽</span>
      </div>

      <ImageGallery :photos="ad.photos" />

      <div class="panel stack">
        <p class="ad-card__meta">
          <template v-if="sectionName">{{ sectionName }} — </template>{{ categoryName }} ·
          {{ cityName }}
        </p>
        <p>{{ ad.description }}</p>
        <div class="stack stack--compact">
          <p class="text-muted">Контакты</p>
          <p>{{ ad.contacts }}</p>
        </div>
        <p class="text-muted">Опубликовано {{ publishedDate }}</p>
      </div>
    </article>
  </section>
</template>
