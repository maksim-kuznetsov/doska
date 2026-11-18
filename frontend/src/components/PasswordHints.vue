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
  <ul class="hints" aria-label="Требования к паролю">
    <li
      v-for="rule in rules"
      :key="rule.key"
      class="hints__item"
      :class="`hints__item--${stateOf(rule.met)}`"
      :data-state="stateOf(rule.met)"
    >
      <span class="hints__mark" aria-hidden="true">{{ markOf(stateOf(rule.met)) }}</span>
      {{ rule.label }}
    </li>
  </ul>
</template>

<style scoped>
.hints {
  margin: 0.5rem 0 0;
  padding: 0;
  list-style: none;
  font-size: 0.875rem;
}

.hints__item {
  display: flex;
  gap: 0.4rem;
}

.hints__item--neutral {
  opacity: 0.7;
}

.hints__item--met {
  color: #1a7f37;
}

.hints__item--unmet {
  color: #b42318;
}

.hints__mark {
  width: 1em;
}
</style>