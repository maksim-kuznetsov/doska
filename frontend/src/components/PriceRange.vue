<script setup lang="ts">
/** Единый контрол диапазона цены: минимум, разделитель и максимум. */
withDefaults(
  defineProps<{
    min: string
    max: string
    label?: string
  }>(),
  { label: 'Цена, ₽' },
)

const emit = defineEmits<{
  (event: 'update:min', value: string): void
  (event: 'update:max', value: string): void
}>()

function onMin(event: Event): void {
  emit('update:min', (event.target as HTMLInputElement).value)
}

function onMax(event: Event): void {
  emit('update:max', (event.target as HTMLInputElement).value)
}
</script>

<template>
  <div class="field">
    <span class="field__label">{{ label }}</span>
    <div class="price-range" role="group" :aria-label="label">
      <input
        class="price-range__input"
        type="number"
        min="0"
        inputmode="numeric"
        aria-label="Цена от"
        :value="min"
        @input="onMin"
      />
      <span class="price-range__separator" aria-hidden="true"></span>
      <input
        class="price-range__input"
        type="number"
        min="0"
        inputmode="numeric"
        aria-label="Цена до"
        :value="max"
        @input="onMax"
      />
      <span class="price-range__currency" aria-hidden="true">₽</span>
    </div>
  </div>
</template>