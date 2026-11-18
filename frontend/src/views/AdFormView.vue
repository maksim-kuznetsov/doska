<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { ApiError, api, type CategoryOption, type CityOption } from '../api/client'

const router = useRouter()

const cities = ref<CityOption[]>([])
const categories = ref<CategoryOption[]>([])
const error = ref('')
const catalogError = ref('')
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

async function submit(): Promise<void> {
  error.value = ''
  submitting.value = true

  try {
    await api.ads.create({
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