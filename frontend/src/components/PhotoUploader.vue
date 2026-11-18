<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'

import { ACCEPTED_PHOTO_TYPES, MAX_PHOTOS, selectPhotos } from '../api/photos'

/** Зона загрузки фотографий: выбор, превью, счётчик и удаление до отправки. */
const props = withDefaults(
  defineProps<{
    files: File[]
    max?: number
    inputId?: string
  }>(),
  { max: MAX_PHOTOS, inputId: 'photos' },
)

const emit = defineEmits<{ (event: 'update:files', value: File[]): void }>()

const previews = ref<string[]>([])
const error = ref('')

function revokeAll(): void {
  previews.value.forEach((url) => URL.revokeObjectURL(url))
  previews.value = []
}

watch(
  () => props.files,
  (files) => {
    revokeAll()
    previews.value = files.map((file) => URL.createObjectURL(file))
  },
  { immediate: true },
)

onBeforeUnmount(revokeAll)

function onInput(event: Event): void {
  const input = event.target as HTMLInputElement

  if (input.files) {
    const result = selectPhotos(props.files, Array.from(input.files))
    emit('update:files', result.accepted)
    error.value = result.rejected.join('; ')
  }

  input.value = ''
}

function remove(index: number): void {
  const next = [...props.files]
  next.splice(index, 1)
  error.value = ''
  emit('update:files', next)
}
</script>

<template>
  <div class="photo-uploader">
    <div class="field">
      <span class="field__label">Фотографии ({{ files.length }}/{{ max }})</span>
      <label class="photo-uploader__drop" :for="inputId">
        <span class="photo-uploader__title">Добавьте фотографии</span>
        <span class="photo-uploader__hint">
          JPEG, PNG или WebP, до 5 МБ, от 1 до {{ max }} фотографий.
        </span>
        <input
          :id="inputId"
          class="photo-uploader__input"
          type="file"
          :accept="ACCEPTED_PHOTO_TYPES.join(',')"
          multiple
          @change="onInput"
        />
      </label>
    </div>

    <p v-if="error" class="field__error" role="alert">{{ error }}</p>

    <ul v-if="previews.length > 0" class="photo-uploader__previews" data-test="photo-previews">
      <li v-for="(url, index) in previews" :key="url" class="photo-uploader__preview">
        <img class="photo-uploader__image" :src="url" alt="Превью фотографии" />
        <button
          type="button"
          class="photo-uploader__remove"
          aria-label="Удалить фотографию"
          @click="remove(index)"
        >
          ×
        </button>
      </li>
    </ul>
  </div>
</template>