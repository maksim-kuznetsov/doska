/**
 * Копия `ApiError` из `api/client` для моков в тестах.
 *
 * Класс повторяет продуктивный контракт (`name`, `status`, сообщение), чтобы
 * `instanceof` и обработка ошибок в компонентах работали одинаково.
 */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

/**
 * Фабрика модуля-заглушки `api/client`.
 *
 * Возвращает класс `ApiError` и переданный объект `api`. Спека оставляет
 * короткий `vi.mock(path, () => createApiClientMock({ ... }))`, а тело класса
 * и форма модуля живут в одном месте.
 */
export function createApiClientMock<T extends object>(
  api: T,
): { ApiError: typeof ApiError; api: T } {
  return { ApiError, api }
}
