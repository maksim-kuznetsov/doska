<script setup lang="ts">
/** Элемент сетки коротких полей; `wide` занимает всю ширину секции. */
withDefaults(defineProps<{ wide?: boolean }>(), { wide: false })
</script>

<template>
  <div class="form-grid__item" :class="{ 'form-grid__item--wide': wide }">
    <slot />
  </div>
</template>