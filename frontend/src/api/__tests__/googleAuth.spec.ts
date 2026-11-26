import { describe, expect, it } from 'vitest'

import { googleErrorMessage, googleLinkUrl, googleStartUrl } from '../googleAuth'

describe('googleAuth', () => {
  it('строит ссылку входа с закодированным адресом возврата', () => {
    expect(googleStartUrl('/ads?page=2')).toBe(
      '/api/auth/google/start?redirect=%2Fads%3Fpage%3D2',
    )
    expect(googleStartUrl(null)).toBe('/api/auth/google/start?redirect=%2F')
    expect(googleStartUrl('')).toBe('/api/auth/google/start?redirect=%2F')
  })

  it('строит ссылку привязки', () => {
    expect(googleLinkUrl()).toBe('/api/auth/google/link')
  })

  it('переводит коды ошибок в понятные сообщения', () => {
    expect(googleErrorMessage('blocked')).toContain('заблокирован')
    expect(googleErrorMessage('conflict')).toContain('уже привязан')
    expect(googleErrorMessage('unverified')).toContain('не подтвердил')
    expect(googleErrorMessage('session')).toContain('Сессия')
    expect(googleErrorMessage('denied')).toContain('отменён')
    expect(googleErrorMessage('failed')).toContain('Не удалось войти')
    expect(googleErrorMessage(undefined)).toContain('Не удалось войти')
    expect(googleErrorMessage(['bad'])).toContain('Не удалось войти')
  })
})