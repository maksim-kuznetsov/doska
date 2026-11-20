<script setup lang="ts">
import { ref } from 'vue'

import type { AdPhotoSummary } from '../api/client'
import PhotoViewer from './PhotoViewer.vue'

/** Галерея фотографий объявления. */
defineProps<{ photos: AdPhotoSummary[] }>()

const activeIndex = ref<number | null>(null)

function open(index: number, event: MouseEvent): void {
  const opener = event.currentTarget as HTMLElement | null
  opener?.focus()
  activeIndex.value = index
}

function close(): void {
  activeIndex.value = null
}
</script>

<template>
  <ul v-if="photos.length > 0" class="gallery" aria-label="Фотографии объявления">
    <li v-for="(photo, index) in photos" :key="photo.id">
      <button
        type="button"
        class="gallery__button"
        :aria-label="`Открыть фотографию ${photo.sortOrder} в полном размере`"
        @click="open(index, $event)"
      >
        <img class="gallery__image" :src="photo.thumbnailUrl" :alt="`Фотография ${photo.sortOrder}`" />
      </button>
    </li>
  </ul>

  <PhotoViewer
    v-if="activeIndex !== null"
    :photos="photos"
    :start-index="activeIndex"
    @close="close"
  />
</template>
