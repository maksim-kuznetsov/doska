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

    await expect(api.ads.get('missing')).rejects.toBeInstanceOf(ApiError)
  })
})
