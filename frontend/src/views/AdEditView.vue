<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import {
  ApiError,
  api,
  type AdPhotoSummary,
  type CityOption,
  type SectionOption,
} from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import FormErrors from '../components/FormErrors.vue'
import FormGrid from '../components/FormGrid.vue'
import FormGridItem from '../components/FormGridItem.vue'
import FormLayout from '../components/FormLayout.vue'
import FormSection from '../components/FormSection.vue'
import ImageGallery from '../components/ImageGallery.vue'
import SelectField from '../components/SelectField.vue'
import SubsectionSelect from '../components/SubsectionSelect.vue'
import TextAreaField from '../components/TextAreaField.vue'
import TextField from '../components/TextField.vue'
import type { FormError } from '../components/formErrors'

const route = useRoute()
const router = useRouter()
const adId = String(route.params.id)

const cities = ref<CityOption[]>([])
const sections = ref<SectionOption[]>([])
const photos = ref<AdPhotoSummary[]>([])
const error = ref('')
const loading = ref(true)
const submitting = ref(false)
const formErrors = ref<FormError[]>([])

const form = reactive({
  title: '',
  description: '',
  price: '',
  cityId: '',
  categoryId: '',
  contacts: '',
})

onMounted(async () => {
  try {
    const [ad, cityList, sectionList] = await Promise.all([
      api.ads.get(adId),
      api.catalog.cities(),
      api.catalog.sections(),
    ])

    cities.value = cityList
    sections.value = sectionList
    photos.value = ad.photos
    form.title = ad.title
    form.description = ad.description
    form.price = String(ad.price)
    form.cityId = ad.cityId
    form.categoryId = ad.categoryId
    form.contacts = ad.contacts
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось загрузить объявление.'
  } finally {
    loading.value = false
  }
})

function validate(): FormError[] {
  const errors: FormError[] = []

  if (form.title.trim().length === 0) {
    errors.push({ fieldId: 'ad-title', label: 'Укажите заголовок.' })
  }

  if (form.description.trim().length === 0) {
    errors.push({ fieldId: 'ad-description', label: 'Добавьте описание.' })
  }

  if (form.price.trim().length === 0) {
    errors.push({ fieldId: 'ad-price', label: 'Укажите цену.' })
  }

  if (!form.cityId) {
    errors.push({ fieldId: 'ad-city', label: 'Выберите город.' })
  }

  if (!form.categoryId) {
    errors.push({ fieldId: 'ad-category', label: 'Выберите подраздел.' })
  }

  if (form.contacts.trim().length === 0) {
    errors.push({ fieldId: 'ad-contacts', label: 'Укажите контакты.' })
  }

  return errors
}

async function submit(): Promise<void> {
  error.value = ''
  formErrors.value = validate()

  if (formErrors.value.length > 0) {
    return
  }

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
      <h1 class="page__title">Редактировать объявление</h1>
      <RouterLink class="btn btn--secondary btn--compact" to="/ads/mine">
        К моим объявлениям
      </RouterLink>
    </div>

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <form v-if="!loading" @submit.prevent="submit">
      <FormLayout>
        <FormErrors :errors="formErrors" />

        <FormSection title="Описание">
          <TextField
            v-model="form.title"
            label="Заголовок"
            input-id="ad-title"
            :maxlength="200"
            required
          />
          <TextAreaField
            v-model="form.description"
            label="Описание"
            input-id="ad-description"
            :rows="5"
            required
          />
        </FormSection>

        <FormSection title="Размещение">
          <FormGrid>
            <FormGridItem>
              <TextField
                v-model="form.price"
                label="Цена, ₽"
                input-id="ad-price"
                type="number"
                min="0"
                step="1"
                required
              />
            </FormGridItem>
            <FormGridItem>
              <SelectField v-model="form.cityId" label="Город" input-id="ad-city" required>
                <option v-for="city in cities" :key="city.id" :value="city.id">
                  {{ city.name }}
                </option>
              </SelectField>
            </FormGridItem>
            <FormGridItem wide>
              <SubsectionSelect
                v-model="form.categoryId"
                input-id="ad-category"
                :sections="sections"
                required
              />
            </FormGridItem>
            <FormGridItem wide>
              <TextField
                v-model="form.contacts"
                label="Контакты"
                input-id="ad-contacts"
                :maxlength="500"
                required
              />
            </FormGridItem>
          </FormGrid>
        </FormSection>

        <FormSection v-if="photos.length > 0" title="Фотографии">
          <ImageGallery :photos="photos" />
        </FormSection>

        <template #aside>
          <p class="form-layout__aside-text">
            Изменение активного объявления без доверия отправит его на повторную проверку.
          </p>
          <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>
          <div class="form-layout__actions">
            <AppButton variant="primary" type="submit" block :disabled="submitting">
              {{ submitting ? 'Сохраняем…' : 'Сохранить' }}
            </AppButton>
          </div>
        </template>
      </FormLayout>
    </form>
  </section>
</template>