import { ApiError } from './client'

/** Единый источник текста ошибки: сообщение `ApiError`, иначе переданный fallback. */
export function messageOf(cause: unknown, fallback: string): string {
  return cause instanceof ApiError ? cause.message : fallback
}
