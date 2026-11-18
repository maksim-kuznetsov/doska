<script setup lang="ts">
/** Подпись, поле и текст ошибки или подсказки, связанные через id. */
defineProps<{
  label: string
  inputId: string
  hint?: string
  error?: string
}>()
</script>

<template>
  <div class="field" :class="{ 'field--error': Boolean(error) }">
    <label class="field__label" :for="inputId">{{ label }}</label>
    <slot />
    <p v-if="error" class="field__error" :id="`${inputId}-error`">{{ error }}</p>
    <p v-else-if="hint" class="field__hint" :id="`${inputId}-hint`">{{ hint }}</p>
  </div>
</template>