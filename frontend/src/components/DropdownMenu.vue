<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

/**
 * Выпадающее меню: раскрывается наведением и нажатием, удерживает указатель
 * на пути от триггера к панели, закрывается по Escape, клику вне и выбору пункта.
 */
const props = withDefaults(
  defineProps<{
    panelId: string
    align?: 'start' | 'end'
    closeDelay?: number
  }>(),
  { align: 'start', closeDelay: 120 },
)

const open = ref(false)
const root = ref<HTMLElement | null>(null)
let closeTimer: ReturnType<typeof setTimeout> | undefined

function cancelClose(): void {
  if (closeTimer !== undefined) {
    clearTimeout(closeTimer)
    closeTimer = undefined
  }
}

function close(): void {
  cancelClose()
  open.value = false
}

function toggle(): void {
  cancelClose()
  open.value = !open.value
}

function onPointerEnter(): void {
  cancelClose()
  open.value = true
}

function onPointerLeave(): void {
  cancelClose()
  closeTimer = setTimeout(() => {
    open.value = false
  }, props.closeDelay)
}

function onDocumentPointerDown(event: PointerEvent): void {
  if (root.value && !root.value.contains(event.target as Node)) {
    close()
  }
}

function onDocumentKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    close()
  }
}

onMounted(() => {
  document.addEventListener('pointerdown', onDocumentPointerDown)
  document.addEventListener('keydown', onDocumentKeydown)
})

onBeforeUnmount(() => {
  cancelClose()
  document.removeEventListener('pointerdown', onDocumentPointerDown)
  document.removeEventListener('keydown', onDocumentKeydown)
})
</script>

<template>
  <div
    ref="root"
    class="dropdown"
    :class="{ 'dropdown--end': align === 'end' }"
    @pointerenter="onPointerEnter"
    @pointerleave="onPointerLeave"
  >
    <slot name="trigger" :open="open" :toggle="toggle" :close="close" />
    <div v-if="open" :id="panelId" class="dropdown__panel">
      <div class="dropdown__surface">
        <slot :close="close" />
      </div>
    </div>
  </div>
</template>