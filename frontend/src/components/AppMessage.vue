<script setup lang="ts">
/** Сообщение над содержимым страницы: успех, ошибка или информация. */
withDefaults(
  defineProps<{
    variant?: 'success' | 'error' | 'info' | 'warning'
  }>(),
  { variant: 'info' },
)
</script>

<template>
  <div
    class="message"
    :class="`message--${variant}`"
    :role="variant === 'error' ? 'alert' : 'status'"
  >
    <span class="message__icon" aria-hidden="true">
      {{ variant === 'success' ? '✓' : variant === 'error' ? '!' : 'i' }}
    </span>
    <span><slot /></span>
  </div>
</template>