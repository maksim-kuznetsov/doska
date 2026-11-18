<script setup lang="ts">
import { computed } from 'vue'

import AppField from './AppField.vue'
import CharCounter from './CharCounter.vue'

/** Текстовое поле формы с подписью, подсказкой, ошибкой и счётчиком. */
const props = withDefaults(
  defineProps<{
    modelValue: string | number
    label: string
    inputId: string
    type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'url' | 'tel'
    placeholder?: string
    maxlength?: number
    min?: number | string
    max?: number | string
    step?: number | string
    autocomplete?: string
    required?: boolean
    disabled?: boolean
    hint?: string
    error?: string
  }>(),
  {
    type: 'text',
    required: false,
    disabled: false,
  },
)

const emit = defineEmits<{ (event: 'update:modelValue', value: string): void }>()

const describedBy = computed(() => {
  if (props.error) return `${props.inputId}-error`
  if (props.hint) return `${props.inputId}-hint`
  return undefined
})

function onInput(event: Event): void {
  emit('update:modelValue', (event.target as HTMLInputElement).value)
}
</script>

<template>
  <AppField :label="label" :input-id="inputId" :hint="hint" :error="error">
    <input
      :id="inputId"
      class="input"
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :maxlength="maxlength"
      :min="min"
      :max="max"
      :step="step"
      :autocomplete="autocomplete"
      :required="required"
      :disabled="disabled"
      :aria-invalid="Boolean(error)"
      :aria-describedby="describedBy"
      @input="onInput"
    />
    <CharCounter v-if="maxlength !== undefined" :current="String(modelValue).length" :max="maxlength" />
  </AppField>
</template>