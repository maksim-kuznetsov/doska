<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

import AppButton from './AppButton.vue'

/**
 * Переиспользуемый модальный диалог: заголовок, содержимое и действия
 * подтверждения и отмены. Закрывается по Esc и клику вне окна.
 */
const props = withDefaults(
  defineProps<{
    title: string
    confirmLabel?: string
    cancelLabel?: string
    confirmDisabled?: boolean
    confirmVariant?: 'primary' | 'danger'
  }>(),
  {
    confirmLabel: 'Подтвердить',
    cancelLabel: 'Отмена',
    confirmDisabled: false,
    confirmVariant: 'primary',
  },
)

const emit = defineEmits<{ (event: 'confirm'): void; (event: 'cancel'): void }>()

const dialogRef = ref<HTMLDialogElement | null>(null)
const opener = (typeof document === 'undefined' ? null : document.activeElement) as HTMLElement | null
const closed = ref(false)

function close(): void {
  if (closed.value) {
    return
  }

  closed.value = true

  const dialog = dialogRef.value
  if (dialog && typeof dialog.close === 'function') {
    dialog.close()
  }

  emit('cancel')
}

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    event.preventDefault()
    close()
  }
}

function onDialogClick(event: MouseEvent): void {
  if (event.target === dialogRef.value) {
    close()
  }
}

function onConfirm(): void {
  if (props.confirmDisabled) {
    return
  }

  emit('confirm')
}

onMounted(() => {
  const dialog = dialogRef.value

  if (!dialog) {
    return
  }

  if (typeof dialog.showModal === 'function') {
    dialog.showModal()
  } else {
    dialog.setAttribute('open', '')
  }
})

onBeforeUnmount(() => {
  opener?.focus()
})
</script>

<template>
  <dialog
    ref="dialogRef"
    class="modal-dialog"
    :aria-label="title"
    @click="onDialogClick"
    @keydown="onKeydown"
    @cancel.prevent="close"
    @close="close"
  >
    <h2 class="modal-dialog__title">{{ title }}</h2>

    <div class="modal-dialog__body">
      <slot />
    </div>

    <div class="modal-dialog__actions">
      <AppButton size="compact" variant="secondary" @click="close">{{ cancelLabel }}</AppButton>
      <AppButton
        size="compact"
        :variant="confirmVariant"
        :disabled="confirmDisabled"
        @click="onConfirm"
      >
        {{ confirmLabel }}
      </AppButton>
    </div>
  </dialog>
</template>
