<script setup lang="ts">
import { ref } from 'vue'

import type { SectionOption } from '../api/client'

/**
 * Дерево активных разделов и подразделов в шапке. Раскрывается кнопкой-бургером
 * рядом с брендом; выбор подраздела ведёт в ленту с фильтром.
 */
defineProps<{
  sections: SectionOption[]
}>()

const open = ref(false)
</script>

<template>
  <div class="category-menu">
    <button
      type="button"
      class="category-menu__toggle"
      aria-label="Категории"
      aria-controls="category-tree"
      :aria-expanded="open"
      @click="open = !open"
    >
      ☰
    </button>

    <div v-if="open" id="category-tree" class="category-menu__panel" data-test="category-tree">
      <p v-if="sections.length === 0" class="muted">Категорий пока нет.</p>

      <ul v-else class="category-menu__sections">
        <li v-for="section in sections" :key="section.id">
          <span class="category-menu__section">{{ section.name }}</span>
          <ul class="category-menu__subsections">
            <li v-for="subsection in section.subsections" :key="subsection.id">
              <RouterLink
                :to="{ name: 'home', query: { categoryId: subsection.id } }"
                @click="open = false"
              >
                {{ subsection.name }}
              </RouterLink>
            </li>
          </ul>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.category-menu {
  position: relative;
}

.category-menu__toggle {
  padding: 0.35rem 0.6rem;
  border: 1px solid currentColor;
  border-radius: 0.4rem;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

.category-menu__panel {
  position: absolute;
  z-index: 10;
  top: calc(100% + 0.5rem);
  left: 0;
  min-width: 16rem;
  max-height: 70vh;
  overflow: auto;
  padding: 0.75rem 1rem;
  border: 1px solid currentColor;
  border-radius: 0.5rem;
  background: Canvas;
  color: CanvasText;
  box-shadow: 0 0.5rem 1.5rem rgb(0 0 0 / 15%);
}

.category-menu__sections,
.category-menu__subsections {
  margin: 0;
  padding: 0;
  list-style: none;
}

.category-menu__sections {
  display: grid;
  gap: 0.6rem;
}

.category-menu__section {
  font-weight: 600;
}

.category-menu__subsections {
  padding-left: 0.75rem;
}

.category-menu__subsections a {
  color: inherit;
}
</style>