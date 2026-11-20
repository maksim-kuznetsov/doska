<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

import type { AdPhotoSummary } from '../api/client'

/** Всплывающее окно просмотра фотографий объявления в полном размере. */
const props = withDefaults(
  defineProps<{
    photos: AdPhotoSummary[]
    startIndex?: number
  }>(),
  { startIndex: 0 },
)

const emit = defineEmits<{ (event: 'close'): void }>()

const dialogRef = ref<HTMLDialogElement | null>(null)
const opener = (typeof document === 'undefined' ? null : document.activeElement) as HTMLElement | null
const closed = ref(false)

function normalize(index: number): number {
  if (props.photos.length === 0) {
    return 0
  }

  return ((index % props.photos.length) + props.photos.length) % props.photos.length
}

const current = ref(normalize(props.startIndex))
const hasMany = computed(() => props.photos.length > 1)
const activePhoto = computed(() => props.photos[current.value] ?? null)
const position = computed(() => current.value + 1)

function next(): void {
  if (hasMany.value) {
    current.value = (current.value + 1) % props.photos.length
  }
}

function prev(): void {
  if (hasMany.value) {
    current.value = (current.value - 1 + props.photos.length) % props.photos.length
  }
}

function goTo(index: number): void {
  current.value = normalize(index)
}

function close(): void {
  if (closed.value) {
    return
  }

  closed.value = true

  const dialog = dialogRef.value
  if (dialog && typeof dialog.close === 'function') {
    dialog.close()
  }

  emit('close')
}

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    event.preventDefault()
    close()
  } else if (event.key === 'ArrowRight') {
    event.preventDefault()
    next()
  } else if (event.key === 'ArrowLeft') {
    event.preventDefault()
    prev()
  }
}

function onDialogClick(event: MouseEvent): void {
  if (event.target === dialogRef.value) {
    close()
  }
}

onMounted(() => {
  const dialog = dialogRef.value

  if (!dialog) {
    return
  }

  if (typeof dialog.showModal === 'function') {
    dialog.showModal()
  } else {
    dialog.setAttribute('open', '')
  }
})

onBeforeUnmount(() => {
  opener?.focus()
})
</script>

<template>
  <dialog
    ref="dialogRef"
    class="photo-viewer"
    aria-label="Просмотр фотографий объявления"
    @click="onDialogClick"
    @keydown="onKeydown"
    @cancel.prevent="close"
    @close="close"
  >
    <div class="photo-viewer__stage">
      <img
        v-if="activePhoto"
        class="photo-viewer__image"
        :src="activePhoto.url"
        :alt="`Фотография ${activePhoto.sortOrder}`"
      />
    </div>

    <button
      v-if="hasMany"
      type="button"
      class="photo-viewer__nav photo-viewer__nav--prev"
      aria-label="Предыдущая фотография"
      @click="prev"
    >
      ‹
    </button>
    <button
      v-if="hasMany"
      type="button"
      class="photo-viewer__nav photo-viewer__nav--next"
      aria-label="Следующая фотография"
      @click="next"
    >
      ›
    </button>

    <button
      type="button"
      class="photo-viewer__close"
      aria-label="Закрыть просмотр"
      @click="close"
    >
      ×
    </button>

    <div class="photo-viewer__footer">
      <p class="photo-viewer__counter">{{ position }} из {{ photos.length }}</p>

      <ul v-if="hasMany" class="photo-viewer__thumbs" aria-label="Миниатюры фотографий">
        <li v-for="(photo, index) in photos" :key="photo.id">
          <button
            type="button"
            class="photo-viewer__thumb"
            :class="{ 'photo-viewer__thumb--active': index === current }"
            :aria-current="index === current ? 'true' : undefined"
            :aria-label="`Фотография ${photo.sortOrder}`"
            @click="goTo(index)"
          >
            <img :src="photo.thumbnailUrl" :alt="`Фотография ${photo.sortOrder}`" />
          </button>
        </li>
      </ul>
    </div>
  </dialog>
</template>
