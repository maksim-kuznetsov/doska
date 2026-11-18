<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { ApiError, api, type CategoryOption, type CityOption } from '../api/client'
import { ACCEPTED_PHOTO_TYPES, MAX_PHOTOS, selectPhotos } from '../api/photos'

const router = useRouter()

const cities = ref<CityOption[]>([])
const categories = ref<CategoryOption[]>([])
const error = ref('')
const photoError = ref('')
const catalogError = ref('')
const submitting = ref(false)
const photos = ref<File[]>([])
const previews = ref<string[]>([])

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
    const [cityList, categoryList] = await Promise.all([
      api.catalog.cities(),
      api.catalog.categories(),
    ])
    cities.value = cityList
    categories.value = categoryList
  } catch {
    catalogError.value = 'Не удалось загрузить города и категории.'
  }
})

onBeforeUnmount(() => {
  previews.value.forEach((url) => URL.revokeObjectURL(url))
})

function onFileInput(event: Event): void {
  const input = event.target as HTMLInputElement

  if (input.files) {
    const result = selectPhotos(photos.value, Array.from(input.files))
    const added = result.accepted.slice(photos.value.length)

    photos.value = result.accepted
    added.forEach((file) => previews.value.push(URL.createObjectURL(file)))
    photoError.value = result.rejected.join('; ')
  }

  input.value = ''
}

function removePhoto(index: number): void {
  URL.revokeObjectURL(previews.value[index])
  previews.value.splice(index, 1)
  photos.value.splice(index, 1)
  photoError.value = ''
}

async function submit(): Promise<void> {
  error.value = ''

  if (photos.value.length === 0) {
    error.value = 'Приложите хотя бы одну фотографию.'
    return
  }

  submitting.value = true

  try {
    const data = new FormData()
    data.append('title', form.title)
    data.append('description', form.description)
    data.append('price', String(form.price))
    data.append('cityId', form.cityId)
    data.append('categoryId', form.categoryId)
    data.append('contacts', form.contacts)
    photos.value.forEach((file) => data.append('photos', file))

    await api.ads.create(data)
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
    <h1>Подать объявление</h1>

    <p v-if="catalogError" class="error">{{ catalogError }}</p>

    <form class="form" @submit.prevent="submit">
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
          <option disabled value="">Выберите город</option>
          <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Подраздел</span>
        <select v-model="form.categoryId" class="field__input" required>
          <option disabled value="">Выберите подраздел</option>
          <option v-for="category in categories" :key="category.id" :value="category.id">
            {{ category.sectionName }} — {{ category.name }}
          </option>
        </select>
      </label>

      <label class="field">
        <span class="field__label">Контакты</span>
        <input v-model="form.contacts" class="field__input" type="text" maxlength="500" required />
      </label>

      <div class="field">
        <span class="field__label">Фотографии ({{ photos.length }}/{{ MAX_PHOTOS }})</span>
        <input
          class="field__input"
          type="file"
          :accept="ACCEPTED_PHOTO_TYPES.join(',')"
          multiple
          @change="onFileInput"
        />
        <span class="muted">JPEG, PNG или WebP, до 5 МБ, от 1 до 5 фотографий.</span>
        <p v-if="photoError" class="error">{{ photoError }}</p>
      </div>

      <ul v-if="previews.length > 0" class="previews">
        <li v-for="(url, index) in previews" :key="url" class="preview">
          <img :src="url" alt="Превью фотографии" />
          <button type="button" class="button" @click="removePhoto(index)">Удалить</button>
        </li>
      </ul>

      <p class="muted">
        Объявление пользователя без доверия попадёт на проверку. Доверенный автор публикуется сразу.
      </p>

      <p v-if="error" class="error">{{ error }}</p>

      <button class="button button--primary" type="submit" :disabled="submitting">
        {{ submitting ? 'Отправляем…' : 'Отправить' }}
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

.preview {
  display: grid;
  gap: 0.35rem;
  justify-items: start;
}

.preview img {
  width: 8rem;
  height: 6rem;
  object-fit: cover;
  border-radius: 0.4rem;
  border: 1px solid currentColor;
}
</style>