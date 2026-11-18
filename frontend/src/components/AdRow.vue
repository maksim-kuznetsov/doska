<script setup lang="ts">
import type { AdSummary } from '../api/client'
import StatusBadge from './StatusBadge.vue'

/** Строка объявления в личном кабинете и списках. */
defineProps<{ ad: AdSummary }>()
</script>

<template>
  <article class="ad-row" data-test="my-ad">
    <div class="ad-row__media">
      <img
        v-if="ad.photos.length > 0"
        class="ad-row__image"
        :src="ad.photos[0].thumbnailUrl"
        :alt="ad.title"
      />
      <div v-else class="ad-row__placeholder" aria-hidden="true">Нет фото</div>
    </div>

    <div class="ad-row__body">
      <div class="ad-row__head">
        <h2 class="ad-row__title">{{ ad.title }}</h2>
        <StatusBadge :status="ad.status" />
      </div>
      <p class="ad-row__meta">
        <span class="ad-row__price">{{ ad.price }} ₽</span> · {{ ad.contacts }}
      </p>
      <p v-if="ad.moderationReason" class="ad-row__reason">
        Причина: {{ ad.moderationReason }}
      </p>
      <div v-if="$slots.actions" class="ad-row__actions">
        <slot name="actions" />
      </div>
    </div>
  </article>
</template>