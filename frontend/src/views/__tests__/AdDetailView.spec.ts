import { flushPromises } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

const { get, cities, sections } = vi.hoisted(() => ({
  get: vi.fn(),
  cities: vi.fn(),
  sections: vi.fn(),
}))

vi.mock('../../api/client', () =>
  createApiClientMock({
    ads: { get: (id: string) => get(id) },
    catalog: { cities: () => cities(), sections: () => sections() },
  }),
)

import { ApiError } from '../../api/client'
import AdDetailView from '../AdDetailView.vue'

const activeAd = {
  id: 'ad-1',
  title: 'Велосипед',
  description: 'Аккуратный городской велосипед.',
  price: 25000,
  cityId: 'city-1',
  categoryId: 'sub-1',
  contacts: '+7 900 000-00-00',
  status: 'Active',
  moderationReason: null,
  createdAt: '2026-11-19T00:00:00Z',
  publishedAt: '2026-11-19T00:00:00Z',
  photos: [
    {
      id: 'photo-1',
      url: '/api/media/ad-1/one.png',
      thumbnailUrl: '/api/media/ad-1/one_thumb.jpg',
      contentType: 'image/png',
      byteSize: 128,
      width: 8,
      height: 8,
      sortOrder: 1,
    },
  ],
}

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/ads/:id', name: 'item-detail', component: AdDetailView },
]

async function mountView() {
  return mountWithRouter({
    component: AdDetailView,
    routes,
    path: '/ads/ad-1',
  })
}

describe('AdDetailView', () => {
  beforeEach(() => {
    get.mockReset()
    cities.mockReset()
    sections.mockReset()

    cities.mockResolvedValue([{ id: 'city-1', name: 'Москва' }])
    sections.mockResolvedValue([
      { id: 'section-1', name: 'Транспорт', subsections: [{ id: 'sub-1', name: 'Велосипеды' }] },
    ])
  })

  it('показывает данные активного объявления и галерею', async () => {
    get.mockResolvedValue(activeAd)

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Велосипед')
    expect(wrapper.text()).toContain('25000')
    expect(wrapper.text()).toContain('Велосипеды')
    expect(wrapper.findAll('.gallery__image')).toHaveLength(1)
    expect(wrapper.find('.breadcrumbs').text()).toContain('Транспорт')
  })

  it('показывает пустое состояние для недоступного объявления', async () => {
    get.mockRejectedValue(new ApiError(404, 'Объявление не найдено.'))

    const wrapper = await mountView()
    await flushPromises()

    expect(wrapper.find('.empty-state').text()).toContain('Объявление недоступно')
  })

  it('открывает окно просмотра по клику на фотографию', async () => {
    get.mockResolvedValue(activeAd)

    const wrapper = await mountView()
    await flushPromises()

    await wrapper.find('.gallery__button').trigger('click')

    expect(wrapper.find('.photo-viewer').exists()).toBe(true)
    expect(wrapper.find('.photo-viewer__image').attributes('src')).toBe('/api/media/ad-1/one.png')
  })
})
