<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import {
  ApiError,
  api,
  type AdSummary,
  type CityOption,
  type SectionOption,
} from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'
import ImageGallery from '../components/ImageGallery.vue'
import ModalDialog from '../components/ModalDialog.vue'
import TextAreaField from '../components/TextAreaField.vue'
import { useApiAction } from '../composables/useApiAction'
import { useAuthStore } from '../stores/auth'

/** Публичная страница объявления: галерея, цена, описание, контакты и действия. */
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const adId = String(route.params.id)

const ad = ref<AdSummary | null>(null)
const sections = ref<SectionOption[]>([])
const cities = ref<CityOption[]>([])
const loading = ref(true)
const notFound = ref(false)
const error = ref('')

const complaintOpen = ref(false)
const complaintReason = ref('')
const complaintFiled = ref(false)
const submitting = ref(false)
const { error: complaintError, notice: complaintNotice, run: runComplaint } = useApiAction()

const canEdit = computed(() => ad.value?.isOwn === true)
const canComplain = computed(
  () =>
    ad.value !== null &&
    !ad.value.isOwn &&
    ad.value.status === 'Active' &&
    !complaintFiled.value,
)

function openComplaint(): void {
  if (!auth.isAuthenticated) {
    void router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }

  complaintError.value = ''
  complaintNotice.value = ''
  complaintReason.value = ''
  complaintOpen.value = true
}

async function submitComplaint(): Promise<void> {
  if (complaintReason.value.trim().length === 0) {
    complaintError.value = 'Укажите причину жалобы.'
    return
  }

  submitting.value = true

  await runComplaint(
    () => api.complaints.file(adId, complaintReason.value.trim()),
    'Жалоба отправлена.',
    'Не удалось отправить жалобу.',
  )

  submitting.value = false

  if (complaintError.value === '') {
    complaintOpen.value = false
    complaintFiled.value = true
  }
}

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
  const items: Array<{ label: string; to: string }> = [{ label: 'Главная', to: '/' }]

  if (categoryName.value && ad.value) {
    items.push({ label: categoryName.value, to: `/?categoryId=${ad.value.categoryId}` })
  }

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
      error.value = messageOf(cause, 'Не удалось загрузить объявление.')
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
    <AppMessage v-if="complaintNotice" variant="success">{{ complaintNotice }}</AppMessage>

    <EmptyState
      v-if="!loading && notFound"
      title="Объявление недоступно"
      description="Оно снято с публикации, продано или не существует."
    >
      <template #actions>
        <RouterLink class="btn btn--secondary btn--compact" to="/">На главную</RouterLink>
      </template>
    </EmptyState>

    <article v-if="ad" class="item-detail">
      <div class="item-detail__main stack">
        <h1 class="page__title">{{ ad.title }}</h1>

        <ImageGallery :photos="ad.photos" />

        <div class="panel stack">
          <p class="item-card__meta">
            <template v-if="sectionName">{{ sectionName }} — </template>{{ categoryName }} ·
            {{ cityName }}
          </p>
          <p>{{ ad.description }}</p>
          <p class="text-muted">Опубликовано {{ publishedDate }}</p>
        </div>
      </div>

      <aside class="item-detail__aside">
        <div class="panel stack">
          <span class="item-card__price">{{ ad.price }} ₽</span>
          <div class="stack stack--compact">
            <p class="text-muted">Контакты</p>
            <p>{{ ad.contacts }}</p>
          </div>
          <div class="stack stack--compact">
            <RouterLink
              v-if="canEdit"
              class="btn btn--secondary btn--compact"
              :to="`/ads/${ad.id}/edit`"
            >
              Редактировать
            </RouterLink>
            <AppButton
              v-if="canComplain"
              size="compact"
              variant="secondary"
              @click="openComplaint"
            >
              Пожаловаться
            </AppButton>
          </div>
        </div>
      </aside>
    </article>

    <ModalDialog
      v-if="complaintOpen"
      title="Жалоба на объявление"
      confirm-label="Отправить"
      :confirm-disabled="submitting"
      @confirm="submitComplaint"
      @cancel="complaintOpen = false"
    >
      <TextAreaField
        v-model="complaintReason"
        label="Причина жалобы"
        input-id="complaint-reason"
        :rows="4"
        required
        :error="complaintError"
      />
    </ModalDialog>
  </section>
</template>
