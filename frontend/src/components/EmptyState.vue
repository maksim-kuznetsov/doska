<script setup lang="ts">
/** Пустое состояние вместо пустого списка. */
withDefaults(
  defineProps<{
    title?: string
    description?: string
  }>(),
  { title: 'Ничего не найдено', description: '' },
)
</script>

<template>
  <div class="empty-state">
    <p class="empty-state__title">{{ title }}</p>
    <p v-if="description" class="empty-state__description">{{ description }}</p>
    <div v-if="$slots.actions" class="empty-state__actions">
      <slot name="actions" />
    </div>
  </div>
</template>