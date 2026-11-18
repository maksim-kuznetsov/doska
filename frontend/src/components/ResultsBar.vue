<script setup lang="ts">
import type { AdSort } from '../api/client'

/** Строка результатов: число найденных и управление сортировкой. */
defineProps<{
  total: number
  sort: AdSort
}>()

const emit = defineEmits<{ (event: 'update:sort', value: AdSort): void }>()

function onChange(event: Event): void {
  emit('update:sort', (event.target as HTMLSelectElement).value as AdSort)
}
</script>

<template>
  <div class="results-bar">
    <span class="results-bar__count">Найдено объявлений: {{ total }}</span>
    <label class="results-bar__sort">
      Сортировка
      <select class="results-bar__select" :value="sort" @change="onChange">
        <option value="newest">Сначала новые</option>
        <option value="price_asc">Цена: по возрастанию</option>
        <option value="price_desc">Цена: по убыванию</option>
      </select>
    </label>
  </div>
</template>