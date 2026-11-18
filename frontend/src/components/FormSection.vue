<script setup lang="ts">
/** Секция формы: группа полей с видимым заголовком-легендой. */
defineProps<{
  title: string
  description?: string
}>()
</script>

<template>
  <fieldset class="form-section">
    <legend class="form-section__legend">{{ title }}</legend>
    <p v-if="description" class="form-section__description">{{ description }}</p>
    <slot />
  </fieldset>
</template>