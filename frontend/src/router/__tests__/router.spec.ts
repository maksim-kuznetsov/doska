import { describe, expect, it } from 'vitest'

import router from '../index'

describe('Маршруты объявлений и заглушка', () => {
  it('открывает страницу объявления по адресу', () => {
    const resolved = router.resolve('/ads/ad-42')

    expect(resolved.name).toBe('item-detail')
    expect(resolved.params.id).toBe('ad-42')
  })

  it('не подменяет страницей объявления служебные адреса', () => {
    expect(router.resolve('/ads/new').name).toBe('ad-new')
    expect(router.resolve('/ads/mine').name).toBe('ads-mine')
  })

  it('показывает заглушку на неизвестном адресе внутри общего макета', () => {
    const resolved = router.resolve('/нет-такой-страницы')

    expect(resolved.name).toBe('not-found')
    expect(resolved.matched.some((record) => record.path === '/')).toBe(true)
  })

  it('открывает политику конфиденциальности без требования входа', () => {
    const resolved = router.resolve('/privacy')

    expect(resolved.name).toBe('privacy')
    expect(resolved.meta.requiresAuth).toBeUndefined()
    expect(resolved.matched.some((record) => record.path === '/')).toBe(true)
  })

  it('открывает условия использования без требования входа', () => {
    const resolved = router.resolve('/terms')

    expect(resolved.name).toBe('terms')
    expect(resolved.meta.requiresAuth).toBeUndefined()
    expect(resolved.matched.some((record) => record.path === '/')).toBe(true)
  })
})
