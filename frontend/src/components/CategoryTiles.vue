<script setup lang="ts">
import { RouterLink } from 'vue-router'

import type { SectionOption } from '../api/client'

/** Блок разделов и подразделов плитками на главной. */
defineProps<{
  sections: SectionOption[]
}>()
</script>

<template>
  <section class="category-tiles" aria-labelledby="category-tiles-title" data-test="category-tiles">
    <h2 id="category-tiles-title" class="category-tiles__title">Разделы</h2>

    <p v-if="sections.length === 0" class="category-tiles__empty text-muted">
      Разделы пока не добавлены.
    </p>

    <ul v-else class="category-tiles__grid">
      <li v-for="section in sections" :key="section.id" class="category-tile">
        <RouterLink
          class="category-tile__head"
          :to="{ name: 'home', query: { categoryId: section.id } }"
        >
          {{ section.name }}
        </RouterLink>

        <ul v-if="section.subsections.length > 0" class="category-tile__links">
          <li v-for="sub in section.subsections" :key="sub.id">
            <RouterLink
              class="category-tile__link"
              :to="{ name: 'home', query: { categoryId: sub.id } }"
            >
              {{ sub.name }}
            </RouterLink>
          </li>
        </ul>
      </li>
    </ul>
  </section>
</template>
