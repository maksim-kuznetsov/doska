<script setup lang="ts">
import type { AdSummary } from '../api/client'
import StatusBadge from './StatusBadge.vue'

/** Строка объявления в личном кабинете и списках. */
defineProps<{ ad: AdSummary }>()
</script>

<template>
  <article class="item-row" data-test="my-item">
    <div class="item-row__media">
      <img
        v-if="ad.photos.length > 0"
        class="item-row__image"
        :src="ad.photos[0].thumbnailUrl"
        :alt="ad.title"
      />
      <div v-else class="item-row__placeholder" aria-hidden="true">Нет фото</div>
    </div>

    <div class="item-row__body">
      <div class="item-row__head">
        <h2 class="item-row__title">{{ ad.title }}</h2>
        <StatusBadge :status="ad.status" />
      </div>
      <p class="item-row__meta">
        <span class="item-row__price">{{ ad.price }} ₽</span> · {{ ad.contacts }}
      </p>
      <p v-if="ad.moderationReason" class="item-row__reason">
        Причина: {{ ad.moderationReason }}
      </p>
      <div v-if="$slots.actions" class="item-row__actions">
        <slot name="actions" />
      </div>
    </div>
  </article>
</template>