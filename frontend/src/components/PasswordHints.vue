<script setup lang="ts">
import { computed } from 'vue'

/**
 * Интерактивные подсказки к паролю: три правила проверяются в браузере как подсказка,
 * серверная проверка сложности остаётся обязательной.
 */
const props = defineProps<{
  password: string
  confirmation: string
}>()

const touched = computed(
  () => props.password.length > 0 || props.confirmation.length > 0,
)

const rules = computed(() => [
  {
    key: 'length',
    label: 'Не менее 8 символов',
    met: props.password.length >= 8,
  },
  {
    key: 'letters',
    label: 'Не только цифры',
    met: props.password.length > 0 && /[^0-9]/.test(props.password),
  },
  {
    key: 'match',
    label: 'Пароли совпадают',
    met: props.password.length > 0 && props.password === props.confirmation,
  },
])

function stateOf(met: boolean): 'neutral' | 'met' | 'unmet' {
  if (!touched.value) {
    return 'neutral'
  }

  return met ? 'met' : 'unmet'
}

function markOf(state: 'neutral' | 'met' | 'unmet'): string {
  if (state === 'met') {
    return '✓'
  }

  return state === 'unmet' ? '✕' : '•'
}
</script>

<template>
  <ul class="password-hints" aria-label="Требования к паролю">
    <li
      v-for="rule in rules"
      :key="rule.key"
      class="password-hints__item"
      :class="`password-hints__item--${stateOf(rule.met)}`"
      :data-state="stateOf(rule.met)"
    >
      <span class="password-hints__mark" aria-hidden="true">{{ markOf(stateOf(rule.met)) }}</span>
      {{ rule.label }}
    </li>
  </ul>
</template>