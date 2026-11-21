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
    class="item-card"
    :to="{ name: 'item-detail', params: { id: ad.id } }"
    data-test="item-card"
  >
    <div class="item-card__media">
      <img v-if="ad.thumbnailUrl" class="item-card__image" :src="ad.thumbnailUrl" :alt="ad.title" />
      <div v-else class="item-card__placeholder" aria-hidden="true">Нет фото</div>
    </div>

    <div class="item-card__body">
      <span class="item-card__price">{{ ad.price }} ₽</span>
      <h2 class="item-card__title">{{ ad.title }}</h2>
      <p class="item-card__meta">
        {{ ad.sectionName }} — {{ ad.categoryName }} · {{ ad.cityName }}
      </p>
      <p class="item-card__description">{{ ad.description }}</p>
      <p class="item-card__date">{{ formatDate(ad.publishedAt) }}</p>
    </div>
  </RouterLink>
</template>
