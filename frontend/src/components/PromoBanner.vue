<script setup lang="ts">
import { RouterLink } from 'vue-router'

/** Промо-баннер на главной: заголовок и действие подачи объявления. */
withDefaults(
  defineProps<{
    title?: string
    text?: string
    to?: string
  }>(),
  {
    title: 'Подайте объявление за пару минут',
    text: 'Разместите товар или услугу и получайте отклики от покупателей.',
    to: '/ads/new',
  },
)
</script>

<template>
  <section class="promo-banner" data-test="promo-banner">
    <div class="promo-banner__body">
      <h2 class="promo-banner__title">{{ title }}</h2>
      <p class="promo-banner__text">{{ text }}</p>
    </div>
    <RouterLink class="btn btn--primary promo-banner__action" :to="to">
      Подать объявление
    </RouterLink>
  </section>
</template>
