<script setup lang="ts">
import { computed } from 'vue'

import { formatRussianDate } from '../legal/details'

const props = defineProps<{
  title: string
  updatedAt: string
}>()

const updatedLabel = computed(() => formatRussianDate(props.updatedAt))
</script>

<template>
  <section class="page">
    <article class="legal">
      <header class="legal__head">
        <h1 class="legal__title">{{ title }}</h1>
        <p class="legal__updated">Дата редакции: {{ updatedLabel }}</p>
      </header>

      <div class="legal__content">
        <slot />
      </div>
    </article>
  </section>
</template>