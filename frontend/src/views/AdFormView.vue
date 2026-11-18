<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'

import { ApiError, api, type CityOption, type SectionOption } from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import FormErrors from '../components/FormErrors.vue'
import FormGrid from '../components/FormGrid.vue'
import FormGridItem from '../components/FormGridItem.vue'
import FormLayout from '../components/FormLayout.vue'
import FormSection from '../components/FormSection.vue'
import PhotoUploader from '../components/PhotoUploader.vue'
import SelectField from '../components/SelectField.vue'
import SubsectionSelect from '../components/SubsectionSelect.vue'
import TextAreaField from '../components/TextAreaField.vue'
import TextField from '../components/TextField.vue'
import type { FormError } from '../components/formErrors'

const router = useRouter()

const cities = ref<CityOption[]>([])
const sections = ref<SectionOption[]>([])
const error = ref('')
const catalogError = ref('')
const submitting = ref(false)
const photos = ref<File[]>([])
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
    const [cityList, sectionList] = await Promise.all([
      api.catalog.cities(),
      api.catalog.sections(),
    ])
    cities.value = cityList
    sections.value = sectionList
  } catch {
    catalogError.value = 'Не удалось загрузить города и категории.'
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

  if (!form.cityId) {
    errors.push({ fieldId: 'ad-city', label: 'Выберите город.' })
  }

  if (!form.categoryId) {
    errors.push({ fieldId: 'ad-category', label: 'Выберите подраздел.' })
  }

  if (form.contacts.trim().length === 0) {
    errors.push({ fieldId: 'ad-contacts', label: 'Укажите контакты.' })
  }

  if (form.price.trim().length === 0) {
    errors.push({ fieldId: 'ad-price', label: 'Укажите цену.' })
  }

  if (photos.value.length === 0) {
    errors.push({ fieldId: 'ad-photos', label: 'Приложите хотя бы одну фотографию.' })
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
    const data = new FormData()
    data.append('title', form.title)
    data.append('description', form.description)
    data.append('price', form.price)
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
    <div class="page__head">
      <h1 class="page__title">Подать объявление</h1>
    </div>

    <AppMessage v-if="catalogError" variant="error">{{ catalogError }}</AppMessage>

    <form @submit.prevent="submit">
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
              <SelectField
                v-model="form.cityId"
                label="Город"
                input-id="ad-city"
                required
              >
                <option value="" disabled>Выберите город</option>
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

        <FormSection title="Фотографии">
          <PhotoUploader v-model:files="photos" input-id="ad-photos" />
        </FormSection>

        <template #aside>
          <p class="form-layout__aside-text">
            Объявление пользователя без доверия попадёт на проверку. Доверенный автор публикуется
            сразу.
          </p>
          <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>
          <div class="form-layout__actions">
            <AppButton variant="primary" type="submit" block :disabled="submitting">
              {{ submitting ? 'Отправляем…' : 'Отправить' }}
            </AppButton>
          </div>
        </template>
      </FormLayout>
    </form>
  </section>
</template>