<script setup lang="ts">
import type { AdSort, CityOption, SectionOption } from '../api/client'
import AppButton from './AppButton.vue'
import PriceRange from './PriceRange.vue'
import SearchBar from './SearchBar.vue'
import SelectField from './SelectField.vue'

/** Панель фильтров ленты: поиск отдельной строкой, фильтры ровным рядом. */
defineProps<{
  query: string
  categoryId: string
  cityId: string
  minPrice: string
  maxPrice: string
  sort: AdSort
  sections: SectionOption[]
  cities: CityOption[]
}>()

const emit = defineEmits<{
  (event: 'update:query', value: string): void
  (event: 'update:categoryId', value: string): void
  (event: 'update:cityId', value: string): void
  (event: 'update:minPrice', value: string): void
  (event: 'update:maxPrice', value: string): void
  (event: 'submit'): void
  (event: 'reset'): void
}>()
</script>

<template>
  <form class="filter-panel" role="search" @submit.prevent="emit('submit')">
    <SearchBar
      :model-value="query"
      placeholder="Что ищем?"
      @update:model-value="emit('update:query', $event)"
      @submit="emit('submit')"
    />

    <div class="filter-panel__filters">
      <SelectField
        :model-value="categoryId"
        label="Раздел"
        input-id="filter-category"
        @update:model-value="emit('update:categoryId', $event)"
      >
        <option value="">Все разделы</option>
        <optgroup v-for="section in sections" :key="section.id" :label="section.name">
          <option :value="section.id">Все: {{ section.name }}</option>
          <option v-for="sub in section.subsections" :key="sub.id" :value="sub.id">
            {{ sub.name }}
          </option>
        </optgroup>
      </SelectField>

      <SelectField
        :model-value="cityId"
        label="Город"
        input-id="filter-city"
        @update:model-value="emit('update:cityId', $event)"
      >
        <option value="">Все города</option>
        <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
      </SelectField>

      <PriceRange
        :min="minPrice"
        :max="maxPrice"
        @update:min="emit('update:minPrice', $event)"
        @update:max="emit('update:maxPrice', $event)"
      />
    </div>

    <div class="filter-panel__actions">
      <AppButton variant="primary" type="submit">Показать</AppButton>
      <AppButton variant="secondary" type="button" @click="emit('reset')">Сбросить</AppButton>
    </div>
  </form>
</template>