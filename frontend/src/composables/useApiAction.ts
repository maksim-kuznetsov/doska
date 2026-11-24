import { ref, type Ref } from 'vue'

import { messageOf } from '../api/errors'

export interface ApiAction {
  error: Ref<string>
  notice: Ref<string>
  run: (action: () => Promise<unknown>, success: string, fallback: string) => Promise<void>
}

/**
 * Общая обёртка действия над API: очищает сообщения, выполняет действие,
 * показывает уведомление, при необходимости перечитывает данные и показывает ошибку.
 */
export function useApiAction(reload?: () => Promise<void>): ApiAction {
  const error = ref('')
  const notice = ref('')

  async function run(
    action: () => Promise<unknown>,
    success: string,
    fallback: string,
  ): Promise<void> {
    error.value = ''
    notice.value = ''

    try {
      await action()
      notice.value = success
      await reload?.()
    } catch (cause) {
      error.value = messageOf(cause, fallback)
    }
  }

  return { error, notice, run }
}
