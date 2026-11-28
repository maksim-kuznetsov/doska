import { describe, expect, it } from 'vitest'

import { externalErrorMessage, externalLinkUrl, externalStartUrl } from '../externalAuth'

describe('externalAuth', () => {
  it('строит ссылку входа с закодированным адресом возврата', () => {
    expect(externalStartUrl('google', '/ads?page=2')).toBe(
      '/api/auth/google/start?redirect=%2Fads%3Fpage%3D2',
    )
    expect(externalStartUrl('yandex', null)).toBe('/api/auth/yandex/start?redirect=%2F')
    expect(externalStartUrl('yandex', '')).toBe('/api/auth/yandex/start?redirect=%2F')
  })

  it('строит ссылку привязки', () => {
    expect(externalLinkUrl('google')).toBe('/api/auth/google/link')
    expect(externalLinkUrl('yandex')).toBe('/api/auth/yandex/link')
  })

  it('переводит коды ошибок в понятные сообщения', () => {
    expect(externalErrorMessage('google', 'blocked')).toContain('заблокирован')
    expect(externalErrorMessage('google', 'conflict')).toContain('уже привязан')
    expect(externalErrorMessage('yandex', 'unverified')).toContain('не подтвердил')
    expect(externalErrorMessage('google', 'session')).toContain('Сессия')
    expect(externalErrorMessage('google', 'denied')).toContain('отменён')
    expect(externalErrorMessage('yandex', 'failed')).toContain('Не удалось войти')
    expect(externalErrorMessage('google', undefined)).toContain('Не удалось войти')
    expect(externalErrorMessage('google', ['bad'])).toContain('Не удалось войти')
  })

  it('называет провайдера в сообщении', () => {
    expect(externalErrorMessage('yandex', 'denied')).toContain('Яндекс')
    expect(externalErrorMessage('google', 'denied')).toContain('Google')
  })
})
