<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import {
  ApiError,
  api,
  type AdPhotoSummary,
  type CategoryOption,
  type CityOption,
} from '../api/client'

const route = useRoute()
const router = useRouter()
const adId = String(route.params.id)

const cities = ref<CityOption[]>([])
const categories = ref<CategoryOption[]>([])
const photos = ref<AdPhotoSummary[]>([])
const error = ref('')
const loading = ref(true)
const submitting = ref(false)

const form = reactive({
  title: '',
  description: '',
  price: 0,
  cityId: '',
  categoryId: '',
  contacts: '',
})

onMounted(async () => {
  try {
    const [ad, cityList, categoryList] = await Promise.all([
      api.ads.get(adId),
      api.catalog.cities(),
      api.catalog.categories(),
    ])

    cities.value = cityList
    categories.value = categoryList
    photos.value = ad.photos
    form.title = ad.title
    form.description = ad.description
    form.price = ad.price
    form.cityId = ad.cityId
    form.categoryId = ad.categoryId
    form.contacts = ad.contacts
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявление.'
  } finally {
    loading.value = false
  }
})

async function submit(): Promise<void> {
  error.value = ''
  submitting.value = true

  try {
    await api.ads.update(adId, {
      title: form.title,
      description: form.description,
      price: Number(form.price),
      cityId: form.cityId,
      categoryId: form.categoryId,
      contacts: form.contacts,
    })

    await router.push('/ads/mine')
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось сохранить объявление.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1>Редактировать объявление</h1>
      <RouterLink class="button" to="/ads/mine">К моим объявлениям</RouterLink>
    </div>

    <p v-if="loading" class="muted">Загружаем…</p>
    <p v-if="error" class="error">{{ error }}</p>

    <form v-if="!loading" class="form" @submit.prevent="submit">
      <label class="field">
        <span class="field__label">Заголовок</span>
        <input v-model="form.title" class="field__input" type="text" maxlength="200" required />
      </label>

      <label class="field">
        <span class="field__label">Описание</span>
        <textarea v-model="form.description" class="field__input" rows="5" required></textarea>
      </label>

      <label class="field">
        <span class="field__label">Цена, ₽</span>
        <input v-model.number="form.price" class="field__input" type="number" min="0" step="1" required />
      </label>

      <label class="field">
        <span class="field__label">Город</span>
        <select v-model="form.cityId" class="field__input" required>
          <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Подраздел</span>
        <select v-model="form.categoryId" class="field__input" required>
          <option v-for="category in categories" :key="category.id" :value="category.id">
            {{ category.sectionName }} — {{ category.name }}
          </option>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Контакты</span>
        <input v-model="form.contacts" class="field__input" type="text" maxlength="500" required />
      </label>

      <ul v-if="photos.length > 0" class="previews">
        <li v-for="photo in photos" :key="photo.id" class="preview">
          <img :src="photo.thumbnailUrl" alt="Фотография объявления" />
        </li>
      </ul>

      <p class="muted">
        Изменение активного объявления без доверия отправит его на повторную проверку.
      </p>

      <button class="button button--primary" type="submit" :disabled="submitting">
        {{ submitting ? 'Сохраняем…' : 'Сохранить' }}
      </button>
    </form>
  </section>
</template>

<style scoped>
.previews {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.preview img {
  width: 8rem;
  height: 6rem;
  object-fit: cover;
  border-radius: 0.4rem;
  border: 1px solid currentColor;
}
</style>