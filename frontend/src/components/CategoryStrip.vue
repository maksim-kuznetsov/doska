<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'

import type { SectionOption } from '../api/client'
import { computeStripFades } from './categoryStripFades'

/**
 * Постоянная горизонтальная полоса разделов под шапкой. Показывается на
 * настольной ширине; ссылки ведут в ленту с фильтром по разделу.
 */
const props = defineProps<{
  sections: SectionOption[]
}>()

const list = ref<HTMLElement | null>(null)
const fadeStart = ref(false)
const fadeEnd = ref(false)

let observer: ResizeObserver | undefined

/** Пересчитывает затухание краёв по текущему переполнению списка. */
function updateFades(): void {
  const element = list.value
  if (!element) {
    fadeStart.value = false
    fadeEnd.value = false
    return
  }

  const fades = computeStripFades({
    scrollLeft: element.scrollLeft,
    clientWidth: element.clientWidth,
    scrollWidth: element.scrollWidth,
  })

  fadeStart.value = fades.fadeStart
  fadeEnd.value = fades.fadeEnd
}

onMounted(() => {
  updateFades()

  // Наблюдатель доступен не во всех окружениях (например, jsdom в тестах).
  if (typeof ResizeObserver === 'undefined') {
    return
  }

  observer = new ResizeObserver(() => updateFades())
  if (list.value) {
    observer.observe(list.value)
  }
})

onBeforeUnmount(() => {
  observer?.disconnect()
  observer = undefined
})

watch(
  () => props.sections,
  async () => {
    await nextTick()
    updateFades()
  },
)
</script>

<template>
  <nav class="category-strip" aria-label="Разделы" data-test="category-strip">
    <ul
      ref="list"
      class="category-strip__list"
      :class="{
        'category-strip__list--fade-start': fadeStart,
        'category-strip__list--fade-end': fadeEnd,
      }"
      @scroll="updateFades"
    >
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
