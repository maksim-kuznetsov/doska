<script setup lang="ts">
import type { AdPhotoSummary } from '../api/client'

/** Галерея фотографий объявления. */
defineProps<{ photos: AdPhotoSummary[] }>()
</script>

<template>
  <ul v-if="photos.length > 0" class="gallery" aria-label="Фотографии объявления">
    <li v-for="photo in photos" :key="photo.id">
      <img class="gallery__image" :src="photo.thumbnailUrl" :alt="`Фотография ${photo.sortOrder}`" />
    </li>
  </ul>
</template>