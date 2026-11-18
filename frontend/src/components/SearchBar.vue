<script setup lang="ts">
import { ref } from 'vue'

import AppButton from './AppButton.vue'

/** Строка поиска: поле и кнопка в одной строке. */
withDefaults(
  defineProps<{
    modelValue: string
    label?: string
    buttonLabel?: string
    placeholder?: string
  }>(),
  { label: 'Поиск', buttonLabel: 'Найти', placeholder: 'Что ищем?' },
)

const emit = defineEmits<{
  (event: 'update:modelValue', value: string): void
  (event: 'submit'): void
}>()

const searchId = ref(`search-${Math.random().toString(36).slice(2, 9)}`)

function onInput(event: Event): void {
  emit('update:modelValue', (event.target as HTMLInputElement).value)
}
</script>

<template>
  <form class="search-bar" role="search" @submit.prevent="emit('submit')">
    <label class="visually-hidden" :for="searchId">{{ label }}</label>
    <input
      :id="searchId"
      class="input search-bar__input"
      type="search"
      :value="modelValue"
      :placeholder="placeholder"
      @input="onInput"
    />
    <AppButton class="search-bar__button" variant="primary" size="compact" type="submit">
      {{ buttonLabel }}
    </AppButton>
  </form>
</template>