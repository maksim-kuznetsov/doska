<script setup lang="ts">
import { computed } from 'vue'

/** Флажок формы для логического свойства, с подписью, подсказкой и ошибкой. */
const props = withDefaults(
  defineProps<{
    modelValue: boolean
    label: string
    inputId: string
    disabled?: boolean
    hint?: string
    error?: string
  }>(),
  { disabled: false },
)

const emit = defineEmits<{ (event: 'update:modelValue', value: boolean): void }>()

const describedBy = computed(() => {
  if (props.error) return `${props.inputId}-error`
  if (props.hint) return `${props.inputId}-hint`
  return undefined
})

function onChange(event: Event): void {
  emit('update:modelValue', (event.target as HTMLInputElement).checked)
}
</script>

<template>
  <div class="field" :class="{ 'field--error': Boolean(error) }">
    <label class="checkbox-field" :for="inputId">
      <input
        :id="inputId"
        class="checkbox-field__input"
        type="checkbox"
        :checked="modelValue"
        :disabled="disabled"
        :aria-invalid="Boolean(error)"
        :aria-describedby="describedBy"
        @change="onChange"
      />
      <span class="checkbox-field__label">{{ label }}</span>
    </label>
    <p v-if="error" class="field__error" :id="`${inputId}-error`">{{ error }}</p>
    <p v-else-if="hint" class="field__hint" :id="`${inputId}-hint`">{{ hint }}</p>
  </div>
</template>
