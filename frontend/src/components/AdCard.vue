<script setup lang="ts">
import { RouterLink } from 'vue-router'

import type { AdCard as AdCardModel } from '../api/client'

/** Карточка объявления для ленты и списков: ведёт на публичную страницу объявления. */
defineProps<{ ad: AdCardModel }>()

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('ru-RU')
}
</script>

<template>
  <RouterLink
    class="ad-card"
    :to="{ name: 'ad-detail', params: { id: ad.id } }"
    data-test="ad-card"
  >
    <div class="ad-card__media">
      <img v-if="ad.thumbnailUrl" class="ad-card__image" :src="ad.thumbnailUrl" :alt="ad.title" />
      <div v-else class="ad-card__placeholder" aria-hidden="true">Нет фото</div>
    </div>

    <div class="ad-card__body">
      <span class="ad-card__price">{{ ad.price }} ₽</span>
      <h2 class="ad-card__title">{{ ad.title }}</h2>
      <p class="ad-card__meta">
        {{ ad.sectionName }} — {{ ad.categoryName }} · {{ ad.cityName }}
      </p>
      <p class="ad-card__description">{{ ad.description }}</p>
      <p class="ad-card__date">{{ formatDate(ad.publishedAt) }}</p>
    </div>
  </RouterLink>
</template>
