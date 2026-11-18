<script setup lang="ts">
import { nextTick, watch } from 'vue'

import type { FormError } from './formErrors'

/** Сводка ошибок формы: ссылки на поля и фокус на первом неверном поле. */
const props = defineProps<{
  errors: FormError[]
}>()

watch(
  () => props.errors,
  async (errors) => {
    if (errors.length === 0) {
      return
    }

    await nextTick()
    const first = document.getElementById(errors[0].fieldId)
    first?.focus()
  },
  { deep: true },
)

function focusField(fieldId: string): void {
  document.getElementById(fieldId)?.focus()
}
</script>

<template>
  <div v-if="errors.length > 0" class="form-errors" role="alert" data-test="form-errors">
    <p class="form-errors__title">Проверьте заполнение формы</p>
    <ul class="form-errors__list">
      <li v-for="error in errors" :key="error.fieldId">
        <a class="form-errors__link" :href="`#${error.fieldId}`" @click.prevent="focusField(error.fieldId)">
          {{ error.label }}
        </a>
      </li>
    </ul>
  </div>
</template>