<script setup lang="ts">
import { computed } from 'vue'

/** Счётчик символов поля: текущая длина и предел. */
const props = defineProps<{
  current: number
  max: number
}>()

const over = computed(() => props.current > props.max)
</script>

<template>
  <span class="char-counter" :class="{ 'char-counter--over': over }" aria-live="polite">
    {{ current }}/{{ max }}
  </span>
</template>