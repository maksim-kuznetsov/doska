<script setup lang="ts">
import { RouterLink } from 'vue-router'

export interface BreadcrumbItem {
  label: string
  to: string
}

/** Цепочка хлебных крошек для вложенных страниц. */
defineProps<{ items: BreadcrumbItem[] }>()
</script>

<template>
  <nav v-if="items.length > 0" class="breadcrumbs" aria-label="Хлебные крошки">
    <ol class="breadcrumbs__list">
      <li v-for="(item, index) in items" :key="`${item.label}-${index}`" class="breadcrumbs__item">
        <RouterLink :to="item.to">{{ item.label }}</RouterLink>
        <span
          v-if="index < items.length - 1"
          class="breadcrumbs__separator"
          aria-hidden="true"
        >/</span>
      </li>
    </ol>
  </nav>
</template>