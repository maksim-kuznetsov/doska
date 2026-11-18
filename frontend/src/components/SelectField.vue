<script setup lang="ts">
import { computed } from 'vue'

import AppField from './AppField.vue'

/** Выпадающий список формы; варианты передаются слотом (в том числе optgroup). */
const props = withDefaults(
  defineProps<{
    modelValue: string
    label: string
    inputId: string
    required?: boolean
    disabled?: boolean
    hint?: string
    error?: string
  }>(),
  { required: false, disabled: false },
)

const emit = defineEmits<{ (event: 'update:modelValue', value: string): void }>()

const describedBy = computed(() => {
  if (props.error) return `${props.inputId}-error`
  if (props.hint) return `${props.inputId}-hint`
  return undefined
})

function onChange(event: Event): void {
  emit('update:modelValue', (event.target as HTMLSelectElement).value)
}
</script>

<template>
  <AppField :label="label" :input-id="inputId" :hint="hint" :error="error">
    <select
      :id="inputId"
      class="input input--select"
      :value="modelValue"
      :required="required"
      :disabled="disabled"
      :aria-invalid="Boolean(error)"
      :aria-describedby="describedBy"
      @change="onChange"
    >
      <slot />
    </select>
  </AppField>
</template>