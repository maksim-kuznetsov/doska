<script setup lang="ts">
import type { AdPhotoSummary } from '../api/client'
import ImageGallery from './ImageGallery.vue'
import StatusBadge from './StatusBadge.vue'

/** Предпросмотр объявления в очередях модерации: фото, цена, гео, описание и контакты. */
defineProps<{
  photos: AdPhotoSummary[]
  price: number
  cityName: string
  categoryName: string
  description: string
  contacts: string
  status?: string
}>()
</script>

<template>
  <div class="stack" data-test="admin-ad-preview">
    <ImageGallery :photos="photos" />

    <div class="split">
      <span class="item-card__price">{{ price }} ₽</span>
      <StatusBadge v-if="status" :status="status" />
    </div>

    <p class="item-card__meta">{{ categoryName }} · {{ cityName }}</p>
    <p>{{ description }}</p>

    <div class="stack stack--compact">
      <p class="text-muted">Контакты</p>
      <p>{{ contacts }}</p>
    </div>
  </div>
</template>
