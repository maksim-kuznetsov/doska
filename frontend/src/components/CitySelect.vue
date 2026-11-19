<script setup lang="ts">
import { ref } from 'vue'

import type { CityOption } from '../api/client'

/** Выбор города в шапке: выбранное значение уходит в фильтр публичной ленты. */
withDefaults(
  defineProps<{
    modelValue: string
    cities: CityOption[]
    label?: string
  }>(),
  { label: 'Город' },
)

const emit = defineEmits<{ (event: 'update:modelValue', value: string): void }>()

const selectId = ref(`city-${Math.random().toString(36).slice(2, 9)}`)

function onChange(event: Event): void {
  emit('update:modelValue', (event.target as HTMLSelectElement).value)
}
</script>

<template>
  <div class="city-select">
    <label class="visually-hidden" :for="selectId">{{ label }}</label>
    <select
      :id="selectId"
      class="input input--select city-select__select"
      :value="modelValue"
      data-test="city-select"
      @change="onChange"
    >
      <option value="">Все города</option>
      <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
    </select>
  </div>
</template>
