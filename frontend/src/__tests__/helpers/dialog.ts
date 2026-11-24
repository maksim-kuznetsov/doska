import { vi } from 'vitest'

type DialogPrototype = {
  showModal?: () => void
  close?: () => void
}

/**
 * Подменяет методы `HTMLDialogElement.prototype`, которых нет в jsdom.
 *
 * Возвращает шпионы и функцию восстановления, чтобы тесты галереи и просмотра
 * фотографий не дублировали заглушку.
 */
export function stubDialogElement(): {
  showModal: ReturnType<typeof vi.fn>
  close: ReturnType<typeof vi.fn>
  restore: () => void
} {
  const prototype = HTMLDialogElement.prototype as unknown as DialogPrototype
  const showModal = vi.fn()
  const close = vi.fn()

  prototype.showModal = showModal as unknown as () => void
  prototype.close = close as unknown as () => void

  return {
    showModal,
    close,
    restore: () => {
      delete prototype.showModal
      delete prototype.close
    },
  }
}
