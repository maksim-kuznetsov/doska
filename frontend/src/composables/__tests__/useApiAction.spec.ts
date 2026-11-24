import { describe, expect, it, vi } from 'vitest'

import { ApiError } from '../../api/client'
import { useApiAction } from '../useApiAction'

describe('useApiAction', () => {
  it('выполняет действие и показывает уведомление', async () => {
    const { error, notice, run } = useApiAction()
    const action = vi.fn().mockResolvedValue(undefined)

    await run(action, 'Готово.', 'Не удалось выполнить действие.')

    expect(action).toHaveBeenCalledOnce()
    expect(notice.value).toBe('Готово.')
    expect(error.value).toBe('')
  })

  it('очищает прежние сообщения перед действием', async () => {
    const { error, notice, run } = useApiAction()

    await run(() => Promise.reject(new Error('boom')), 'Готово.', 'Не удалось выполнить действие.')
    expect(error.value).toBe('Не удалось выполнить действие.')

    await run(() => Promise.resolve(), 'Снова готово.', 'Не удалось выполнить действие.')

    expect(error.value).toBe('')
    expect(notice.value).toBe('Снова готово.')
  })

  it('перечитывает данные через reload после успеха', async () => {
    const reload = vi.fn().mockResolvedValue(undefined)
    const { run } = useApiAction(reload)

    await run(() => Promise.resolve(), 'Готово.', 'Не удалось выполнить действие.')

    expect(reload).toHaveBeenCalledOnce()
  })

  it('не перечитывает данные при ошибке', async () => {
    const reload = vi.fn().mockResolvedValue(undefined)
    const { run } = useApiAction(reload)

    await run(() => Promise.reject(new Error('boom')), 'Готово.', 'Не удалось выполнить действие.')

    expect(reload).not.toHaveBeenCalled()
  })

  it('показывает сообщение ApiError', async () => {
    const { error, run } = useApiAction()

    await run(
      () => Promise.reject(new ApiError(400, 'Некорректные данные.')),
      'Готово.',
      'Не удалось выполнить действие.',
    )

    expect(error.value).toBe('Некорректные данные.')
  })

  it('показывает fallback для неизвестной ошибки', async () => {
    const { error, run } = useApiAction()

    await run(() => Promise.reject(new Error('boom')), 'Готово.', 'Не удалось выполнить действие.')

    expect(error.value).toBe('Не удалось выполнить действие.')
  })
})
