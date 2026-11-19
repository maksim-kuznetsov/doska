<script setup lang="ts">
import { RouterLink } from 'vue-router'

import type { SectionOption } from '../api/client'
import DropdownMenu from './DropdownMenu.vue'

/**
 * Дерево активных разделов и подразделов в шапке. Раскрывается кнопкой-бургером
 * рядом с брендом; выбор подраздела ведёт в ленту с фильтром.
 */
defineProps<{
  sections: SectionOption[]
}>()
</script>

<template>
  <DropdownMenu panel-id="category-tree-panel">
    <template #trigger="{ open, toggle }">
      <button
        type="button"
        class="dropdown__trigger"
        aria-label="Категории"
        aria-controls="category-tree-panel"
        :aria-expanded="open"
        @click="toggle"
      >
        ☰
      </button>
    </template>

    <template #default="{ close }">
      <div id="category-tree" data-test="category-tree">
        <p v-if="sections.length === 0" class="text-muted">Категорий пока нет.</p>

        <div v-else class="dropdown__group">
          <template v-for="(section, index) in sections" :key="section.id">
            <hr v-if="index > 0" class="dropdown__divider" />
            <div>
              <p class="dropdown__group-title">{{ section.name }}</p>
              <ul class="dropdown__list">
                <li v-for="subsection in section.subsections" :key="subsection.id">
                  <RouterLink
                    class="dropdown__item"
                    :to="{ name: 'home', query: { categoryId: subsection.id } }"
                    @click="close"
                  >
                    {{ subsection.name }}
                  </RouterLink>
                </li>
              </ul>
            </div>
          </template>
        </div>
      </div>
    </template>
  </DropdownMenu>
</template>