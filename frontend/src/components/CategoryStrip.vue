<script setup lang="ts">
import { RouterLink } from 'vue-router'

import type { SectionOption } from '../api/client'

/**
 * Постоянная горизонтальная полоса разделов под шапкой. Показывается на
 * настольной ширине; ссылки ведут в ленту с фильтром по разделу.
 */
defineProps<{
  sections: SectionOption[]
}>()
</script>

<template>
  <nav class="category-strip" aria-label="Разделы" data-test="category-strip">
    <ul class="category-strip__list">
      <li>
        <RouterLink class="category-strip__link" :to="{ name: 'home' }">Все категории</RouterLink>
      </li>
      <li v-for="section in sections" :key="section.id">
        <RouterLink
          class="category-strip__link"
          :to="{ name: 'home', query: { categoryId: section.id } }"
        >
          {{ section.name }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>
