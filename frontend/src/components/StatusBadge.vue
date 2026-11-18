<script setup lang="ts">
import { computed } from 'vue'

/** Бейдж статуса объявления: цвет различает состояния. */
const props = defineProps<{ status: string }>()

const labels: Record<string, string> = {
  OnModeration: 'На проверке',
  Active: 'Активно',
  Sold: 'Продано',
  Archived: 'Архив',
  Rejected: 'Отклонено',
  Hidden: 'Скрыто',
}

const modifiers: Record<string, string> = {
  OnModeration: 'on-moderation',
  Active: 'active',
  Sold: 'sold',
  Archived: 'archived',
  Rejected: 'rejected',
  Hidden: 'hidden',
}

const label = computed(() => labels[props.status] ?? props.status)
const modifier = computed(() => modifiers[props.status] ?? 'archived')
</script>

<template>
  <span class="status-badge" :class="`status-badge--${modifier}`" :data-status="status">
    {{ label }}
  </span>
</template>