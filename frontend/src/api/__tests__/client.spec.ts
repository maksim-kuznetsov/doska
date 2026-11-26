import { afterEach, describe, expect, it, vi } from 'vitest'

import { ApiError, api } from '../client'

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('api.ads.get', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('возвращает объявление по идентификатору', async () => {
    const ad = { id: 'ad-1', title: 'Велосипед', price: 25000 }
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(ad))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.ads.get('ad-1')).resolves.toEqual(ad)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/ads/ad-1',
      expect.objectContaining({ method: 'GET', credentials: 'include' }),
    )
  })

  it('бросает ApiError, когда объявление недоступно', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse({ title: 'Объявление не найдено.' }, 404))
    vi.stubGlobal('fetch', fetchMock)

    const error = await api.ads.get('missing').catch((cause: unknown) => cause)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 404,
      message: 'Объявление не найдено.',
    })
  })

  it('объясняет 413 от прокси, даже если тело не JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response('<html><body>413 Request Entity Too Large</body></html>', {
        status: 413,
        headers: { 'Content-Type': 'text/html' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.ads.get('ad-1')).rejects.toMatchObject({
      status: 413,
      message: expect.stringContaining('5 МБ'),
    })
  })

  it('использует сообщение по статусу для не-JSON ошибки прокси', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response('<html>Bad gateway</html>', {
        status: 502,
        headers: { 'Content-Type': 'text/html' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.ads.get('ad-1')).rejects.toMatchObject({
      status: 502,
      message: 'Ошибка запроса (502).',
    })
  })
})

describe('api.ads.search', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('передаёт дробные цены в инвариантной записи с точкой', async () => {
    const page = { items: [], totalCount: 0, page: 1, pageSize: 20 }
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse(page))
    vi.stubGlobal('fetch', fetchMock)

    await api.ads.search({ minPrice: 100.5, maxPrice: 9999999999.99 })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/ads?minPrice=100.5&maxPrice=9999999999.99',
      expect.objectContaining({ method: 'GET', credentials: 'include' }),
    )
  })
})

describe('api.account', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('сохраняет профиль через PUT и возвращает пользователя', async () => {
    const user = { id: 'user-1', email: 'user@example.com', displayName: 'Иван', contacts: '+7 900' }
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf' }))
      .mockResolvedValueOnce(jsonResponse(user))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.account.updateProfile('Иван', '+7 900')).resolves.toEqual(user)
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/account/profile',
      expect.objectContaining({ method: 'PUT', credentials: 'include' }),
    )
  })

  it('меняет пароль через POST и не требует тела ответа', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse({ token: 'csrf' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.account.changePassword('oldpass1', 'newpass1')).resolves.toBeUndefined()
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/account/password',
      expect.objectContaining({ method: 'POST', credentials: 'include' }),
    )
  })
})
