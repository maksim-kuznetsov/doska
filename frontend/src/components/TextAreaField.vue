<script setup lang="ts">
import { computed } from 'vue'

import AppField from './AppField.vue'
import CharCounter from './CharCounter.vue'

/** Многострочное поле формы. */
const props = withDefaults(
  defineProps<{
    modelValue: string
    label: string
    inputId: string
    rows?: number
    maxlength?: number
    placeholder?: string
    required?: boolean
    disabled?: boolean
    hint?: string
    error?: string
  }>(),
  { rows: 5, required: false, disabled: false },
)

const emit = defineEmits<{ (event: 'update:modelValue', value: string): void }>()

const describedBy = computed(() => {
  if (props.error) return `${props.inputId}-error`
  if (props.hint) return `${props.inputId}-hint`
  return undefined
})

function onInput(event: Event): void {
  emit('update:modelValue', (event.target as HTMLTextAreaElement).value)
}
</script>

<template>
  <AppField :label="label" :input-id="inputId" :hint="hint" :error="error">
    <textarea
      :id="inputId"
      class="input input--textarea"
      :rows="rows"
      :value="modelValue"
      :placeholder="placeholder"
      :maxlength="maxlength"
      :required="required"
      :disabled="disabled"
      :aria-invalid="Boolean(error)"
      :aria-describedby="describedBy"
      @input="onInput"
    ></textarea>
    <CharCounter v-if="maxlength !== undefined" :current="modelValue.length" :max="maxlength" />
  </AppField>
</template>