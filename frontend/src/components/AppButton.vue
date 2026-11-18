<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
    size?: 'normal' | 'compact'
    type?: 'button' | 'submit' | 'reset'
    block?: boolean
    disabled?: boolean
  }>(),
  {
    variant: 'secondary',
    size: 'normal',
    type: 'button',
    block: false,
    disabled: false,
  },
)
</script>

<template>
  <button
    :type="type"
    :disabled="disabled"
    class="btn"
    :class="[`btn--${variant}`, { 'btn--compact': size === 'compact', 'btn--block': block }]"
  >
    <slot />
  </button>
</template>