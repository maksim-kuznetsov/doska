<script setup lang="ts">
import type { SectionOption } from '../api/client'
import SelectField from './SelectField.vue'

/** Один список подразделов, сгруппированный по разделам. */
withDefaults(
  defineProps<{
    modelValue: string
    sections: SectionOption[]
    inputId?: string
    required?: boolean
    error?: string
  }>(),
  { inputId: 'categoryId', required: false },
)

defineEmits<{ (event: 'update:modelValue', value: string): void }>()
</script>

<template>
  <SelectField
    :model-value="modelValue"
    label="Подраздел"
    :input-id="inputId"
    :required="required"
    :error="error"
    hint="Сначала раздел, затем подраздел."
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <option value="" disabled>Выберите подраздел</option>
    <optgroup
      v-for="section in sections"
      :key="section.id"
      :label="section.name"
    >
      <option
        v-for="subsection in section.subsections"
        :key="subsection.id"
        :value="subsection.id"
      >
        {{ subsection.name }}
      </option>
    </optgroup>
  </SelectField>
</template>