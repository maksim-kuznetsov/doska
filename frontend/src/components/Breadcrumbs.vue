<script setup lang="ts">
import { RouterLink } from 'vue-router'

export interface BreadcrumbItem {
  label: string
  to?: string
}

/** Цепочка хлебных крошек для вложенных страниц. */
defineProps<{ items: BreadcrumbItem[] }>()
</script>

<template>
  <nav class="breadcrumbs" aria-label="Хлебные крошки">
    <ol class="breadcrumbs__list">
      <li v-for="(item, index) in items" :key="`${item.label}-${index}`" class="breadcrumbs__item">
        <RouterLink v-if="item.to" :to="item.to">{{ item.label }}</RouterLink>
        <span v-else aria-current="page">{{ item.label }}</span>
        <span
          v-if="index < items.length - 1"
          class="breadcrumbs__separator"
          aria-hidden="true"
        >/</span>
      </li>
    </ol>
  </nav>
</template>